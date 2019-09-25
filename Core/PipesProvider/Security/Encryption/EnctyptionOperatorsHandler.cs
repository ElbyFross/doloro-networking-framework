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
        /// Hashtable that contains generated symmetric keys relative to client's guid.
        /// 
        /// Key - string guid.
        /// Value - Security.Encryption.IEncryptionOperator security descriptor 
        /// that contains generated keys and control expiry operation.
        /// </summary>
        private static readonly Hashtable symmetricKeys = new Hashtable();
        
        /// <summary>
        /// TODO Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        public static void TryToDecrypt (ref UniformQueries.Query query)
        {
            // Drop if encryptor not described.
            if(query.Encryption == null || 
                string.IsNullOrEmpty( query.Encryption.encytpionOperatorCode))
            {
                return;
            }

            // Ancrypt via assymetric key.
            if (query.Encryption.asymmetricEncryption)
            {
                // Decrypt message.
                query.Content = AsymmetricKey.Decrypt(query.Content);
            }
            else
            {
                // Get guid for shared configs.
                string guid = UniformDataOperator.Binary.BinaryHandler.FromByteArray<string>(query.Encryption.configs);

                // Loading encryptor by GUID.
                if(symmetricKeys[guid] is IEncryptionOperator encryptor)
                {
                    // Decrypting message.
                    query.Content = encryptor.Decrypt(query.Content);
                }
            }
        }

        /// <summary>
        /// Trying to encrypt data.
        /// </summary>
        /// <param name="query">Query that's content would be ecrypted.</param>
        /// <param name="encryptionOperator">Operator that woud be used to encryption.</param>
        public static void TryToEncrypt(ref UniformQueries.Query query, IEncryptionOperator encryptionOperator)
        {
            // Validate.
            if (query == null) return;
            if (encryptionOperator == null) return;
            if (query.Content == null) return;

            // Encrypt data.
            query.Content = encryptionOperator.Encrypt(UniformDataOperator.Binary.BinaryHandler.ToByteArray(query.Content));
        }

        /// <summary>
        /// Returning encryption operator suitable for client's token.
        /// Registrate new operator to that token if not found.
        /// </summary>
        /// <typeparam name="T">Operator type.</typeparam>
        /// <param name="guid">GUID related to key.</param>
        /// <returns>AES Encryption operator</returns>
        public static IEncryptionOperator GetSymetricKey<T>(string guid) where T : IEncryptionOperator
        {
            // Trying to find already existed operator.
            if (symmetricKeys[guid] is IEncryptionOperator eo)
            {
                return eo;
            }
            else
            {
                // Initialize new operator
                eo = (IEncryptionOperator)Activator.CreateInstance(typeof(T));

                // Adding operator to hashtable.
                symmetricKeys.Add(guid, eo);

                // Returning created operator as result.
                return eo;
            }
        }

        /// <summary>
        /// Trying to encrypt answer query from data shared with received query.
        /// </summary>
        /// <param name="receivedQuery">Query received from client, that contain ecnryption descriptor.</param>
        /// <param name="toEncrypt">Query that would be ecrypted with enctry data.</param>
        public static void TryToEncryptByReceivedQuery(UniformQueries.Query receivedQuery, UniformQueries.Query toEncrypt)
        {
            if (receivedQuery.Encryption != null &&
               !string.IsNullOrEmpty(receivedQuery.Encryption.encytpionOperatorCode))
            {
                // To valid format.
                receivedQuery.Encryption.encytpionOperatorCode = receivedQuery.Encryption.encytpionOperatorCode.ToLower();

                switch (receivedQuery.Encryption.encytpionOperatorCode)
                {
                    case "rsa":
                        // Encrypt query if requested by "pk" query's param.
                        if (receivedQuery.TryGetParamValue(
                            "pk",
                            out UniformQueries.QueryPart publicKeyProp))
                        {
                            try
                            {
                                // Create new RSA decryptor.
                                RSAEncryptionOperator rsaEncryption = new RSAEncryptionOperator
                                {
                                    SharableData = publicKeyProp.PropertyValueString
                                };

                                // Encrypt with shared RSA encryptor.
                                TryToEncrypt(ref toEncrypt, rsaEncryption);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ecryption failed. Operation terminated. Details: " + ex.Message);
                            }
                        }
                        break;

                    case "aes":
                        // Try to get public key from entry query.
                        try
                        {
                            // Get guid for shared configs.
                            string guid = UniformDataOperator.Binary.BinaryHandler.FromByteArray<string>(receivedQuery.Encryption.configs);

                            // Looking for decryptor.
                            AESEncryptionOperator aesEncryption = (AESEncryptionOperator)symmetricKeys[guid];

                            // Encrypt with shared RSA encryptor.
                            EnctyptionOperatorsHandler.TryToEncrypt(ref toEncrypt, aesEncryption);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ecryption failed. Operation terminated. Details: " + ex.Message);
                        }
                        break;
                }
            }
        }
    }
}
