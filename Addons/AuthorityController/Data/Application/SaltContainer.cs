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

using System.Security.Cryptography;

namespace AuthorityController.Data.Application
{
    [System.Serializable]
    public class SaltContainer
    {
        /// <summary>
        /// Bytes array that can be added to information before hashing to increase entropy.
        /// </summary>
        public byte[] key;

        /// <summary>
        /// Stamp that provide confirmation that salt is valid.
        /// During loading salt will applied to test string and this stamp need to be the same as result.
        /// </summary>
        public byte[] validationStamp;

        public SaltContainer() { }

        /// <summary>
        /// Create salt container with requested salt size.
        /// </summary>
        /// <param name="keySize"></param>
        public SaltContainer(int keySize)
        {
            GenerateNewKey(keySize);
        }
        
        /// <summary>
        /// Generate new key for container with requiered size.
        /// </summary>
        /// <param name="keySize"></param>
        public void GenerateNewKey(int keySize)
        {
            // Generate key.
            key = new byte[keySize];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(key);
            }

            // Generate validation stamp.
            validationStamp = GetStamp();
        }

        /// <summary>
        /// Create a stamp related to current salt.
        /// </summary>
        /// <returns></returns>
        public byte[] GetStamp()
        {
            byte[] hashResult = API.Users.GetHashedPassword("SALT VALIDATION STRING", this);
            return hashResult;
        }

        /// <summary>
        /// Call base operation and compare results.
        /// Relevant result must be equal to stamp.
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            // Get hashed string by using this stamp.
            byte[] hashResult = GetStamp();

            // Comapre length
            if (hashResult.Length != validationStamp.Length)
            {
                return false;
            } 

            // Compare two arrays.
            for(int i = 0; i < hashResult.Length; i++)
            {
                // Compare bytes.
                if (hashResult[i] != validationStamp[i])
                {
                    return false;
                }
            }

            // Validation success.
            return true;
        }
    }
}
