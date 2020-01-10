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
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Timers;

namespace PipesProvider.Security
{
    /// <summary>
    /// A static class that implements API for handling crypto features.
    /// </summary>
    public static class Crypto
    {
        #region Enums
        /// <summary>
        /// Enum  that describe type of SHA hash algorithm.
        /// </summary>
        public enum SHATypes
        {
            /// <summary>
            /// SHA1 hash algorithm.
            /// </summary>
            SHA1,
            /// <summary>
            /// SHA256 hash algorithm.
            /// </summary>
            SHA256,
            /// <summary>
            /// SHA384 hash algorithm.
            /// </summary>
            SHA384,
            /// <summary>
            /// SHA512 hash algorithm.
            /// </summary>
            SHA512
        }
        #endregion

        #region Hash
        /// <summary>
        /// Return the hash of the string.
        /// Use SHA256 as default.
        /// </summary>
        /// <param name="input">Input string for encoding.</param>
        /// <returns>Encoded string.</returns>
        public static string StringToSHA(string input)
        {
            return StringToSHA(input, SHATypes.SHA256);
        }

        /// <summary>
        /// Return hash of string.
        /// </summary>
        /// <param name="input">Input string for encoding.</param>
        /// <param name="type">Encoding algorithm's type.</param>
        /// <returns>Encoded string.</returns>
        public static string StringToSHA(string input, SHATypes type)
        {
            byte[] hashValue = null;
            HashAlgorithm hashAlgorithm = null;

            // Select algorithm
            switch (type)
            {
                case SHATypes.SHA1:
                    hashAlgorithm = SHA1.Create();
                    break;
                case SHATypes.SHA256:
                    hashAlgorithm = SHA256.Create();
                    break;
                case SHATypes.SHA384:
                    hashAlgorithm = SHA384.Create();
                    break;
                case SHATypes.SHA512:
                    hashAlgorithm = SHA512.Create();
                    break;
            }


            // Compute the hash of the fileStream.
            try
            {
                hashValue = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
            catch (Exception ex)
            {
                Console.WriteLine("HASH COMPUTING ERROR: {0}", ex.Message);
            }

            // Dispose unmanaged resource.
            hashAlgorithm.Clear();
            hashAlgorithm.Dispose();

            // Convert byte array to string.
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashValue.Length; i++)
            {
                builder.Append(hashValue[i].ToString("x2"));
            }
            return builder.ToString();
        }
        #endregion
    }
}
