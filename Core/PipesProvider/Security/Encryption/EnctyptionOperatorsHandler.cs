//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipesProvider.Networking.Routing;
using PipesProvider.Security.Encryption.Operators;

namespace PipesProvider.Security.Encryption
{
    /// <summary>
    /// Class that provides members to handle encryption members.
    /// </summary>
    public static class EnctyptionOperatorsHandler
    {
        /// <summary>
        /// Asymmedtric key that would be used to encrypting short message before sending to that server.
        /// </summary>
        public static IEncryptionOperator AsymmetricKey { get; set; } = new RSAEncryptionOperator();

        /// <summary>
        /// Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        /// <param name="asymmetricEO">Operator that would be used to decrypting of symmetric key shared with.</param>
        /// <returns>Result of operation.</returns>
        public static async Task<bool> TryToDecryptAsync(
            UniformQueries.Query query,
            IEncryptionOperator asymmetricEO)
        {
            return await TryToDecryptAsync(query, asymmetricEO, System.Threading.CancellationToken.None);
        }

        /// <summary>
        /// TODO Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        /// <param name="asymmetricEO">Operator that would be used to decrypting of symmetric key shared with.</param>
        /// <param name="cancellationToken">Token that woul be used for termination of async operations.</param>
        /// <returns>Result of operation.</returns>
        public static async Task<bool> TryToDecryptAsync(
            UniformQueries.Query query, 
            IEncryptionOperator asymmetricEO,
            System.Threading.CancellationToken cancellationToken)
        {
            #region Validation
            // Drop if encryptor not described.
            if (query.EncryptionMeta == null || 
                string.IsNullOrEmpty( query.EncryptionMeta.contentEncytpionOperatorCode))
            {
                // Decryption not required so operation is success.
                return true;
            }

            // Validate encryption operators.
            if (asymmetricEO == null) return false;
            #endregion

            // Unifyng 
            query.EncryptionMeta.contentEncytpionOperatorCode = query.EncryptionMeta.contentEncytpionOperatorCode.ToLower();

            // Decrypting symmetric key.
            byte[] symmetricKey = await asymmetricEO.DecryptAsync(
                query.EncryptionMeta.encryptedSymmetricKey,
                cancellationToken);

            // Decrypt content with symmetric operator.
            IEncryptionOperator symmetricEncryptionOperator = null;
            switch(query.EncryptionMeta.contentEncytpionOperatorCode)
            {
                case "aes":
                    symmetricEncryptionOperator = new AESEncryptionOperator() { SharableData = symmetricKey };
                    break;
            }

            // Decrypt data.
            query.Content = await symmetricEncryptionOperator.DecryptAsync(query.Content, cancellationToken);

            return true;
        }

        /// <summary>
        /// Trying to encrypt query.
        /// </summary>
        /// <param name="query">Query for encryption.</param>
        /// <param name="symmetricEncryptionOperatorCode">
        /// Code of operator that would be used to content encryption.
        /// AES by default.
        /// </param>
        /// <param name="asymmetricEecryptionOperator">
        /// Operator that would be used to encrypting of symmetric key shared with a </param>
        /// <param name="cancellationToken">Token for termination of async operations.</param>
        /// <returns>Result of operation. False mean that operation failed.</returns>
        public static async Task<bool> TryToEncryptAsync(
           UniformQueries.Query query,
           string symmetricEncryptionOperatorCode,
           IEncryptionOperator asymmetricEecryptionOperator,
           System.Threading.CancellationToken cancellationToken)
        {
            // Validate
            if (string.IsNullOrEmpty(symmetricEncryptionOperatorCode)) return false;

            // To uniform view.
            symmetricEncryptionOperatorCode = symmetricEncryptionOperatorCode.ToLower();

            // Detect content symetryc encryptor.
            IEncryptionOperator symmetricOperator = null;
            switch (symmetricEncryptionOperatorCode)
            {
                default:
                case "aes":
                    // Try to get public key from entry query.
                    try
                    {
                        // Generate new key for data encryption.
                        symmetricOperator = new AESEncryptionOperator()
                        { EncryptionKey = System.Security.Cryptography.Aes.Create().Key };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ecryption failed. Operation terminated. Details: " + ex.Message);
                    }
                    break;
            }

            // Redirect encryption.
            return await TryToEncryptAsync(
                query, 
                symmetricOperator,
                asymmetricEecryptionOperator,
                cancellationToken);
        }

        /// <summary>
        /// Trying to encrypt data.
        /// </summary>
        /// <param name="query">Query that's content would be ecrypted.</param>
        /// <param name="symmetricEncryptionOperator">Operator that woud be used to content encryption.</param>
        /// <param name="asymmetricEecryptionOperator">
        /// Operator that woud be used to symmetric key encryption.
        /// Operate with public key received from server.
        /// </param>
        /// <param name="cancellationToken">Token for operation termination.</param>
        /// <returns>Result of encrypting. False meaning failed.</returns>
        public static async Task<bool> TryToEncryptAsync(
            UniformQueries.Query query,
            IEncryptionOperator symmetricEncryptionOperator,
            IEncryptionOperator asymmetricEecryptionOperator,
            System.Threading.CancellationToken cancellationToken)
        {
            // Validate.
            if (query == null) return false;
            if (symmetricEncryptionOperator == null) return false;
            if (asymmetricEecryptionOperator == null) return false;
            if (query.Content == null) return false;

            // Encrypt data.
            query.Content = symmetricEncryptionOperator.Encrypt(query.Content);

            // Add encryption meta.
            query.EncryptionMeta = new UniformQueries.Query.EncryptionInfo()
            {
                // Save decryptor marker.
                contentEncytpionOperatorCode = symmetricEncryptionOperator.DecryptionMarker,

                // Encrypt syhmetric key used to data encryption with assymetric key received from server.
                encryptedSymmetricKey = await asymmetricEecryptionOperator.EncryptAsync(
                    symmetricEncryptionOperator.SharableData,
                    cancellationToken)
            };

            return true;
        }

        /// <summary>
        /// Trying to encrypt data.
        /// </summary>
        /// <param name="query">Query that's content would be ecrypted.</param>
        /// <param name="symmetricEncryptionOperator">Operator that woud be used to content encryption.</param>
        /// <param name="asymmetricEecryptionOperator">
        /// Operator that woud be used to symmetric key encryption.
        /// Operate with public key received from server.
        /// </param>
        public static void TryToEncrypt(
            UniformQueries.Query query, 
            IEncryptionOperator symmetricEncryptionOperator, 
            IEncryptionOperator asymmetricEecryptionOperator)
        {
            // Validate.
            if (query == null) return;
            if (symmetricEncryptionOperator == null) return;
            if (asymmetricEecryptionOperator == null) return;
            if (query.Content == null) return;

            // Encrypt data.
            query.Content = symmetricEncryptionOperator.Encrypt(query.Content);

            // Add encryption meta.
            query.EncryptionMeta = new UniformQueries.Query.EncryptionInfo()
            {
                // Save decryptor marker.
                contentEncytpionOperatorCode = symmetricEncryptionOperator.DecryptionMarker,

                // Encrypt syhmetric key used to data encryption with assymetric key received from server.
                encryptedSymmetricKey = asymmetricEecryptionOperator.Encrypt(symmetricEncryptionOperator.SharableData)
            };
        }

        /// <summary>
        /// Trying to encrypt answer query from data shared with received query.
        /// </summary>
        /// <param name="receivedQuery">Query received from client, that contain ecnryption descriptor.</param>
        /// <param name="toEncrypt">Query that would be ecrypted with enctry data.</param>
        /// <param name="cancellationToken">Token for operation termination.</param>
        public static async Task<bool> TryToEncryptByReceivedQueryAsync(
            UniformQueries.Query receivedQuery,
            UniformQueries.Query toEncrypt,
            System.Threading.CancellationToken cancellationToken)
        {
            if (receivedQuery.EncryptionMeta != null &&
               !string.IsNullOrEmpty(receivedQuery.EncryptionMeta.contentEncytpionOperatorCode))
            {
                #region RSA encryptor
                IEncryptionOperator asymmetricOperator;
                // Encrypt query if requested by "pk" query's param.
                if (receivedQuery.TryGetParamValue(
                    "pk", out UniformQueries.QueryPart publicKeyProp))
                {
                    try
                    {
                        // Create new RSA decryptor.
                        asymmetricOperator = new RSAEncryptionOperator
                        {
                            SharableData = publicKeyProp.propertyValue
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ecryption failed. Operation terminated. Details: " + ex.Message);
                    }
                }
                #endregion

                #region Encrypt content
                // To valid format.
                receivedQuery.EncryptionMeta.contentEncytpionOperatorCode = 
                    receivedQuery.EncryptionMeta.contentEncytpionOperatorCode.ToLower();

                // Encrypting data.
                return await TryToEncryptAsync(
                    toEncrypt,
                    receivedQuery.EncryptionMeta.contentEncytpionOperatorCode,
                    new RSAEncryptionOperator() { SharableData = publicKeyProp.propertyValue },
                    cancellationToken);
                #endregion
            }

            // Encryption not required.
            return true;
        }
    }
}
