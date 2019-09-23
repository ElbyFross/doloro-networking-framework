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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipesProvider.Security.Encryption
{
    public static class EnctyptionOperatorsHandler
    {
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

            return null;
        }
    }
}
