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
using System.Threading;
using System.Threading.Tasks;
using PipesProvider.Networking.Routing;
using PipesProvider.Security.Encryption.Operators;
using UniformQueries;

namespace PipesProvider.Security.Encryption
{
    /// <summary>
    /// Class that provides members to handle encryption members.
    /// </summary>
    public static class EnctyptionOperatorsHandler
    {
        /// <summary>
        /// An instance of the current using asymmetric encryption operator.
        /// </summary>
        /// <remarks>
        /// The <see cref="RSAEncryptionOperator"/> by default.
        /// </remarks>
        public static IEncryptionOperator AsymmetricEO { get; set; } = new RSAEncryptionOperator();

        /// <summary>
        /// Table that contains all types of registred asymmetric encription operators.
        /// Accessable by the operators' codes defiead with the 
        /// <see cref="EncryptionOperatorCodeAttribute"/>.
        /// </summary>
        private readonly static Hashtable AsymmetricOperators = new Hashtable();

        /// <summary>
        /// Table that contains all registred symmetric encription operators.
        /// Accessable by the operators' codes defiead with the 
        /// <see cref="EncryptionOperatorCodeAttribute"/>.
        /// </summary>
        private readonly static Hashtable SymmetricOperators = new Hashtable();

        /// <summary>
        /// Looking for the operators.
        /// </summary>
        static EnctyptionOperatorsHandler()
        {
            // Load query's processors.
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Console.WriteLine("\nDETECTED ENCRIPTION OPERATORS:");
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();

                    // Get all types for assembly.
                    foreach (Type type in assembly.GetTypes())
                    {
                        try
                        {
                            // Check if this type is subclass with implemented IEncryptionOperator interface.
                            if (type.GetInterface(typeof(IEncryptionOperator).FullName) != null &&
                                !type.IsAbstract &&
                                !type.IsInterface)
                            {
                                // Skip if type was replaced by other.
                                if (UniformDataOperator.AssembliesManagement.Modifiers.TypeReplacer.IsReplaced(type))
                                {
                                    continue;
                                }

                                RegistrateOperator(type);
                                Console.WriteLine("{0}", type.FullName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ecription operator asseblies loading failed (eoh10): {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ecription operator asseblies loading failed (2): {eoh10}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Looking for an asymetric operator among registred.
        /// </summary>
        /// <param name="code">An unique identifier of a target operator. Example: "rsa".</param>
        /// <returns>An encryption operator that found by the key.</returns>
        /// <exception cref="NotSupportedException">
        /// Occurs in case if requested opertor not registred.
        /// </exception>
        public static IEncryptionOperator InstantiateAsymmetricOperator(string code)
        {
            return InstantiateOperatorByCode(code, AsymmetricOperators);
        }

        /// <summary>
        /// Looking for an symetric operator among registred.
        /// </summary>
        /// <param name="code">An unique identifier of a target operator. Example: "aes".</param>
        /// <returns>An encryption operator that found by the key.</returns>
        /// <exception cref="NotSupportedException">
        /// Occurs in case if requested opertor not registred.
        /// </exception>
        public static IEncryptionOperator InstantiateSymmetricOperator(string code)
        {
            return InstantiateOperatorByCode(code, SymmetricOperators);
        }

        /// <summary>
        /// Registrate a value at the target table.
        /// </summary>
        /// <param name="encryptionOperator">
        /// Operator's that must be registread at the system.
        /// </param>
        public static void RegistrateOperator(Type encryptionOperator)
        {
            EncryptionOperatorType algorithmType;
            string code;
            try
            {
                // Loading metadata.
                code = GetOperatorCode(encryptionOperator);
                algorithmType = GetOperatorAlgorithmType(encryptionOperator);
            }
            catch
            {
                // Log erros.
                Console.WriteLine(
                    "Operator \"" + encryptionOperator.FullName +
                    "\" not loaded. Be sure that it has defined" +
                    " `EncryptionOperatorCode` and `EncryptionOperatorType`" +
                    " attributes.");

                // Drop.
                return;
            }

            // Defining a table that should contains the operator.
            Hashtable table;
            if (algorithmType == EncryptionOperatorType.Asymmetric)
            {
                table = AsymmetricOperators;
            }
            else
            {
                table = SymmetricOperators;
            }

            // Check if the operator already registrad at the table.
            bool operatorContaied = table.ContainsKey(code);

            // Setting a value to the table.
            if (operatorContaied)
            {
                Console.WriteLine("Ecnryption operator with \"" + code +
                    "\" code was overrided by the \"" +
                    encryptionOperator.FullName + "\" type");

                // Overriding the value to the table.
                table[code] = encryptionOperator;
            }
            else
            {
                //Console.WriteLine("New Ecnryption operator with \"" + code +
                //    "\" code defined by the \"" +
                //    encryptionOperator.FullName + "\" type");

                // Adding to the table.
                table.Add(code, encryptionOperator);
            }
        }

        /// <summary>
        /// Looking for the code defined to the operator 
        /// by using the <see cref="EncryptionOperatorCodeAttribute"/>.
        /// </summary>
        /// <param name="encryptionOperator">Target encryption operator.</param>
        /// <returns>An operator's code.</returns>
        /// <exception cref="NullReferenceException">
        /// Operator not found or has no defined `EncryptionOperatorCodeAttribute`
        /// </exception>
        public static string GetOperatorCode(IEncryptionOperator encryptionOperator)
        {
            return GetOperatorCode(encryptionOperator.GetType());
        }

        /// <summary>
        /// Looking for the code defined to the operator 
        /// by using the <see cref="EncryptionOperatorCodeAttribute"/>.
        /// </summary>
        /// <param name="encryptionOperatorType">Target encryption operator's type.</param>
        /// <returns>An operator's code.</returns>
        /// <exception cref="NullReferenceException">
        /// Operator not found or has no defined `EncryptionOperatorCodeAttribute`
        /// </exception>
        public static string GetOperatorCode(Type encryptionOperatorType)
        {
            UniformDataOperator.AssembliesManagement.MembersHandler.
                  TryToGetAttribute<EncryptionOperatorCodeAttribute>(
                  encryptionOperatorType,
                  out EncryptionOperatorCodeAttribute attribute);

            return attribute.Code;
        }

        
        /// <summary>
        /// Looking for the algorithm type defined to the operator 
        /// by using the <see cref="EncryptionOperatorTypeAttribute"/>.
        /// </summary>
        /// <param name="encryptionOperator">Target encryption operator.</param>
        /// <returns>An operator's algorithm type.</returns>
        /// <exception cref="NullReferenceException">
        /// Operator not found or has no defined `EncryptionOperatorTypeAttribute`
        /// </exception>
        public static EncryptionOperatorType GetOperatorAlgorithmType(
            IEncryptionOperator encryptionOperator)
        {
            return GetOperatorAlgorithmType(encryptionOperator.GetType());
        }

        /// <summary>
        /// Looking for the algorithm type defined to the operator 
        /// by using the <see cref="EncryptionOperatorTypeAttribute"/>.
        /// </summary>
        /// <param name="encryptionOperatorType">Target encryption operator type.</param>
        /// <returns>An operator's algorithm type.</returns>
        /// <exception cref="NullReferenceException">
        /// Operator not found or has no defined `EncryptionOperatorTypeAttribute`
        /// </exception>
        public static EncryptionOperatorType GetOperatorAlgorithmType(
            Type encryptionOperatorType)
        {
            UniformDataOperator.AssembliesManagement.MembersHandler.
                TryToGetAttribute<EncryptionOperatorTypeAttribute>(
                encryptionOperatorType,
                out EncryptionOperatorTypeAttribute attribute);

            return attribute.OperatorType;
        }

        /// <summary>
        /// Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        /// <param name="asymmetricEO">Operator that would be used to decrypting of symmetric key shared with.</param>
        /// <returns>Result of operation.</returns>
        public static async Task<bool> TryToDecryptAsync(
            Query query,
            IEncryptionOperator asymmetricEO)
        {
            return await TryToDecryptAsync(query, asymmetricEO, System.Threading.CancellationToken.None);
        }

        /// <summary>
        /// Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        /// <param name="asymmetricEO">Operator that would be used to decrypting of symmetric key shared with.</param>
        /// <param name="cancellationToken">Token that woul be used for termination of async operations.</param>
        /// <returns>Result of operation.</returns>
        public static async Task<bool> TryToDecryptAsync(
            Query query, 
            IEncryptionOperator asymmetricEO,
            CancellationToken cancellationToken)
        {
            #region Validation
            // Drop if encryptor not described.
            if (query.EncryptionMeta == null || 
                string.IsNullOrEmpty(query.EncryptionMeta.contentEncrytpionOperatorCode))
            {
                // Decryption not required so operation is success.
                return true;
            }

            // Validate encryption operators.
            if (asymmetricEO == null) return false;
            #endregion

            // Unifyng 
            query.EncryptionMeta.contentEncrytpionOperatorCode = 
                query.EncryptionMeta.contentEncrytpionOperatorCode.ToLower();

            // Decrypting symmetric key.
            byte[] symmetricKey = await asymmetricEO.DecryptAsync(
                query.EncryptionMeta.encryptedSymmetricKey,
                cancellationToken);

            // Creating an operator by the code.
            var symmetricOperator = InstantiateSymmetricOperator(
                query.EncryptionMeta.contentEncrytpionOperatorCode);

            // Applying a key.
            symmetricOperator.SharableData = symmetricKey;

            // Decrypt data.
            query.Content = await symmetricOperator.DecryptAsync(query.Content, cancellationToken);

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
           Query query,
           string symmetricEncryptionOperatorCode,
           IEncryptionOperator asymmetricEecryptionOperator,
           CancellationToken cancellationToken)
        {
            // Validate
            if (string.IsNullOrEmpty(symmetricEncryptionOperatorCode)) return false;

            // To uniform view.
            symmetricEncryptionOperatorCode = symmetricEncryptionOperatorCode.ToLower();

            // Detect content symetryc encryptor.
            var symmetricOperator = InstantiateSymmetricOperator(symmetricEncryptionOperatorCode);

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
            Query query,
            IEncryptionOperator symmetricEncryptionOperator,
            IEncryptionOperator asymmetricEecryptionOperator,
            CancellationToken cancellationToken)
        {
            // Validate.
            if (query == null) return false;
            if (symmetricEncryptionOperator == null) return false;
            if (asymmetricEecryptionOperator == null) return false;
            if (query.Content == null) return false;

            // Encrypt data.
            query.Content = symmetricEncryptionOperator.Encrypt(query.Content);

            // Add encryption meta.
            query.EncryptionMeta = new Query.EncryptionInfo()
            {
                // Save decryptor marker.
                contentEncrytpionOperatorCode = GetOperatorCode(symmetricEncryptionOperator),

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
            Query query, 
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
            query.EncryptionMeta = new Query.EncryptionInfo()
            {
                // Save decryptor marker.
                contentEncrytpionOperatorCode = GetOperatorCode(symmetricEncryptionOperator),

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
            Query receivedQuery,
            Query toEncrypt,
            CancellationToken cancellationToken)
        {
            if (receivedQuery.EncryptionMeta != null &&
               !string.IsNullOrEmpty(receivedQuery.EncryptionMeta.contentEncrytpionOperatorCode))
            {
                #region RSA encryptor
                IEncryptionOperator asymmetricOperator = null;
                // Encrypt query if requested by "pk" query's param.
                if (receivedQuery.TryGetParamValue(
                    "pk", out UniformQueries.QueryPart publicKeyProp))
                {
                    try
                    {
                        // Create a new operator with the same type as main.
                        asymmetricOperator = (IEncryptionOperator)Activator.
                            CreateInstance(AsymmetricEO.GetType());

                        // Applying a key.
                        asymmetricOperator.SharableData = publicKeyProp.propertyValue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ecryption failed. Operation terminated. Details: " + ex.Message);
                    }
                }
                #endregion

                #region Encrypt content
                // To valid format.
                receivedQuery.EncryptionMeta.contentEncrytpionOperatorCode = 
                    receivedQuery.EncryptionMeta.contentEncrytpionOperatorCode.ToLower();

                // Encrypting data.
                return await TryToEncryptAsync(
                    toEncrypt,
                    receivedQuery.EncryptionMeta.contentEncrytpionOperatorCode,
                    asymmetricOperator,
                    cancellationToken);
                #endregion
            }

            // Encryption not required.
            return true;
        }

        /// <summary>
        /// Looking for an asymetric operator among registred.
        /// </summary>
        /// <param name="key">A key of a target operator. Example: "rsa".</param>
        /// <param name="table">A table where will looking the operator.</param>
        /// <returns>An encryption operator that found by the key.</returns>
        /// <exception cref="NotSupportedException">
        /// Occurs in case if requested opertor not registred.
        /// </exception>
        private static IEncryptionOperator InstantiateOperatorByCode(string key, Hashtable table)
        {
            // Trying to get operator.
            if (table[key] is Type op)
            {
                // Return it if found.
                return (IEncryptionOperator)Activator.CreateInstance(op);
            }

            // Throwing exception in case if not found by the key.
            throw new NotSupportedException(
                "Encryption operator with \"" + key + "\" key not registred.");
        }
    }
}
