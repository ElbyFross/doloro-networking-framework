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

using System.Text;
using System.Security.Cryptography;

namespace AuthorityController.Data.Application
{
    /// <summary>
    /// Provides salt container that dat
    /// </summary>
    [System.Serializable]
    public class SaltContainer
    {
        /// <summary>
        /// A bytes array that can be added to information before hashing to increase entropy.
        /// </summary>
        public byte[] key;

        /// <summary>
        /// A stamp that provides confirmation that salt is valid.
        /// During loading a salt will applied to a test string and this stamp must to be the same as result.
        /// </summary>
        public byte[] validationStamp;

        /// <summary>
        /// Instiniate default salt container.
        /// </summary>
        public SaltContainer() { }

        /// <summary>
        /// Create salt container with requested salt size.
        /// </summary>
        /// <param name="keySize">A size of the key.</param>
        public SaltContainer(int keySize)
        {
            GenerateNewKey(keySize);
        }
        
        /// <summary>
        /// Generate new key for container with requiered size.
        /// </summary>
        /// <param name="keySize">A size of the key.</param>
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
            byte[] hashResult = GetHashedPassword("SALT VALIDATION STRING", this);
            return hashResult;
        }

        /// <summary>
        /// Call base operation and compare results.
        /// Relevant result must be equal to stamp.
        /// </summary>
        /// <returns>A result of validation.</returns>
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

        /// <summary>
        /// Convert password to heshed and salted.
        /// </summary>
        /// <param name="input">Password recived from user.</param>
        /// <param name="salt">Salt that would be used to increase entropy.</param>
        /// <returns>A hash of the password.</returns>
        public static byte[] GetHashedPassword(string input, SaltContainer salt)
        {
            // Get recived password to byte array.
            byte[] plainText = Encoding.UTF8.GetBytes(input);

            // Create hash profider.
            using (HashAlgorithm algorithm = new SHA256Managed())
            {
                // Allocate result array.
                byte[] plainTextWithSaltBytes =
                  new byte[plainText.Length + salt.key.Length];

                // Copy input to result array.
                for (int i = 0; i < plainText.Length; i++)
                {
                    plainTextWithSaltBytes[i] = plainText[i];
                }

                // Add salt to array.
                for (int i = 0; i < salt.key.Length; i++)
                {
                    plainTextWithSaltBytes[plainText.Length + i] = salt.key[i];
                }

                // Get hash of salted array.
                return algorithm.ComputeHash(plainTextWithSaltBytes);
            }
        }
    }
}
