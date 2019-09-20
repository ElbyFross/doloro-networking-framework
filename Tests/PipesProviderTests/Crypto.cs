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
using System.Threading;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;

namespace PipesProviderTests
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class Crypto
    {
        /// <summary>
        /// Generated encrypted key for AES crypto provider.
        /// </summary>
        public static byte[] AESEncryptionKey
        {
            get
            {
                if (aesCSP == null)
                {
                    aesCSP = new AesCryptoServiceProvider()
                    {
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };
                    aesCSP.GenerateKey();
                }

                return aesCSP.Key;
            }
        }

        /// <summary>
        /// Crypto provider using to generate encryption key during session.
        /// </summary>
        private static AesCryptoServiceProvider aesCSP;

        [TestCleanup]
        public void Cleanup()
        {
            aesCSP.Clear();
            aesCSP.Dispose();
        }


        [TestMethod]
        public void AESEncryption()
        {
            var roundtrip = "This is the data I am encrypting.  There are many like it but this is my encryption.";

            bool operationCompleted = false;
            string decrypted = null;

            // Encrypt data.
            PipesProvider.Security.Crypto.AESEncryptAsync(
                Encoding.UTF8.GetBytes(roundtrip), 
                AESEncryptionKey, 
                delegate (byte[] encryptedData) // Delegate that would operate encrypted data.
                {
                    // Decrypt data.
                    PipesProvider.Security.Crypto.AESDecryptAsync(
                        encryptedData,
                        AESEncryptionKey,
                        delegate (byte[] decryptedData) // Delegate that would operate decrypted data.
                        {
                            // Encode binary data to string.
                            decrypted = Encoding.UTF8.GetString(decryptedData).TrimEnd('\0');

                            // Mark operation as complete.
                            operationCompleted = true;
                        },
                        System.Threading.CancellationToken.None);
                }, 
                System.Threading.CancellationToken.None);

            // Whait util operation complete.
            while (!operationCompleted)
            {
                Thread.Sleep(5);
            }
            Assert.IsTrue(roundtrip == decrypted);
        }
    }
}
