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
        /// Key data exculted from data array during decryption process. 
        /// Can be used to refecrs encryption with the same params.
        /// </summary>
        public class EncryptionMeta
        {

        }

        /// <summary>
        /// TODO Trying to decrypt data.
        /// </summary>
        /// <param name="query">Binary data that can contain encryption descryptor.</param>
        /// <returns>Key data exculted from data array during decryption process. 
        /// Can be used to refecrs encryption with the same params.</returns>
        public static EncryptionMeta TryToDecrypt (ref UniformQueries.Query query)
        {
            /*// Trying to receive encryption operator code from header.
            if(!UniformQueries.API.TryGetParamValue("crypto", out string ecryptorHeader, encryptorHeader))
            {
                // Drop decrypting if not crypto header not exist.
                return null;
            }

            // TODO Find target EcnrytionOperator.
            IEncryptionOperator encryptionOperator = null;

            encryptionOperator.de*/

            throw new NotImplementedException();
        }

        /// <summary>
        /// TODO Trying to encrypt data.
        /// </summary>
        /// <param name="query">Query that's content would be ecrypted.</param>
        /// <param name="encryptionOperator">Operator that woud be used to encryption.</param>
        public static void TryToEncrypt(ref UniformQueries.Query query, IEncryptionOperator encryptionOperator)
        {
            throw new NotImplementedException();
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
        /// TODO Detecting encryption operator from query.
        /// </summary>
        /// <param name="receivedQuery"></param>
        /// <returns></returns>
        public static IEncryptionOperator DetectEncryption(UniformQueries.Query receivedQuery)
        {
            if(receivedQuery.Encryption == null || 
               string.IsNullOrEmpty(receivedQuery.Encryption.encytpionOperatorCode))
            {
                return null;
            }

            if(receivedQuery.Encryption.asymmetricEncryption)
            {
                throw new NotImplementedException();
            }
        }
    }
}
