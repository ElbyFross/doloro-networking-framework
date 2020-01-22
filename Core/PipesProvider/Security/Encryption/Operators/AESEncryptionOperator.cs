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
using System.Security.Cryptography;
using System.IO;
using System.Timers;
using PipesProvider.Networking.Routing;

namespace PipesProvider.Security.Encryption.Operators
{
    /// <summary>
    /// Ecryuption operator that provides API to ecryption by AES algorithm.
    /// </summary>
    [EncryptionOperatorCode("aes")]
    [EncryptionOperatorType(EncryptionOperatorType.Symmetric)]
    public class AESEncryptionOperator : IEncryptionOperator
    {
        /// <summary>
        /// Encoder that provides concertation query from string to byte array.
        /// </summary>
        public Encoding Encoder { get; set; } = Encoding.Default;

        /// <summary>
        /// Is current encryption provider is valid and can be used in transmission.
        /// </summary>
        public bool IsValid
        {
            get
            {
                // If crypto provider expired.
                if (_SecretKey == null || _SecretKey.Count() == 0)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Always cause NotSupportedException.
        /// Key must be applied like a new for every transmission.
        /// </summary>
        public int SessionTime { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <summary>
        /// Always cause NotSupportedException.
        /// Key must be applied like a new for every transmission.
        /// </summary>
        public DateTime ExpiryTime { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <summary>
        /// Object that contains suitable data that can be used to encryption\decryption of data.
        /// Generate new if null.
        /// </summary>
        public object EncryptionKey
        {
            get
            {
                if(_SecretKey == null)
                {
                    using (var aes = Aes.Create())
                    {
                        _SecretKey = aes.Key;
                    }
                }
                return _SecretKey;
            }
            set { _SecretKey = value as byte[]; }
        }
        
        /// <summary>
        /// Object that contains data suitable for encryption of received transmission data.
        /// </summary>
        public object DecryptionKey
        {
            get { return _SecretKey; }
            set { _SecretKey = value as byte[]; }
        }

        /// <summary>
        /// Bufer that contains binary ley that would be used for encryption\decryption.
        /// </summary>
        private byte[] _SecretKey;

        /// <summary>
        /// Public keys in binary format allowed to sharing in message format.
        /// </summary>
        public byte[] SharableData
        {
            get
            {               
                return _SecretKey;
            }
            set
            {
                _SecretKey = value;
            }
        }

        /// <summary>
        /// Decrypting string message.
        /// </summary>
        /// <param name="message">Messege to decryption.</param>
        /// <returns>Decrypted message</returns>
        public string Decrypt(string message)
        { 
            // Conver message to byte array.
            byte[] bytedMessage = Encoder.GetBytes(message);
            //byte[] bytedMessage = Convert.FromBase64String(message);

            // Encrypt byte array.
            byte[] encryptedMessage = Decrypt(bytedMessage);

            // Create decrypted string.
            if (encryptedMessage != null)
            {
                // Convert bytes array to string
                string decryptedMessageString = Encoder.GetString(encryptedMessage);
                //string decryptedMessageString = Convert.ToBase64String(encryptedMessage);

                // Log
                Console.WriteLine("DECRYPTED: {0}\n", decryptedMessageString);

                return decryptedMessageString;
            }
            else
            {
                // Return entry message cause decryotion failed.
                return message;
            }
        }

        /// <summary>
        /// Decrypting binary data.
        /// </summary>
        /// <param name="data">Binary data to decryption.</param>
        /// <returns>Decrypted data.</returns>
        public byte[] Decrypt(byte[] data)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                // Configurating provider.
                provider.Key = _SecretKey;
                provider.Padding = PaddingMode.PKCS7;
                provider.Mode = CipherMode.CBC;

                // Opening stream.
                using (var ms = new MemoryStream(data))
                {
                    // Receiving initialization vector
                    byte[] buffer = new byte[16];
                    ms.Read(buffer, 0, 16);
                    provider.IV = buffer;

                    // Decrypting message
                    using (var decryptor = provider.CreateDecryptor(provider.Key, provider.IV))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] decrypted = new byte[data.Length - 16];
                            cs.Read(decrypted, 0, decrypted.Length);
                            return decrypted;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronous decrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to decryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Decrypted data.</returns>
        public async Task<byte[]> DecryptAsync(byte[] data, System.Threading.CancellationToken cancellationToken)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                // Configurating provider.
                provider.Key = _SecretKey;
                provider.Padding = PaddingMode.PKCS7;
                provider.Mode = CipherMode.CBC;

                // Opening stream.
                using (var ms = new MemoryStream(data))
                {
                    // Receiving initialization vector
                    byte[] buffer = new byte[16];
                    ms.Read(buffer, 0, 16);
                    provider.IV = buffer;

                    // Decrypting message
                    using (var decryptor = provider.CreateDecryptor(provider.Key, provider.IV))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] decrypted = new byte[data.Length - 16];
                            await cs.ReadAsync(decrypted, 0, decrypted.Length, cancellationToken);
                            return decrypted;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Encrypting string message.
        /// </summary>
        /// <param name="message">Messege to encryption.</param>
        /// <returns>Encrypted message</returns>
        public string Encrypt(string message)
        { 
            // Conver message to byte array.
            byte[] bytedMessage = Encoder.GetBytes(message);
            //byte[] bytedMessage = Convert.FromBase64String(message);

            // Encrypt byte array.
            byte[] encryptedMessage = Encrypt(bytedMessage);

            // Create encrypted string.
            string encryptedMessageString = Encoder.GetString(encryptedMessage);
            //string encryptedMessageString = Convert.ToBase64String(encryptedMessage);

            //Console.WriteLine("ENCRYPTED TO:\n{0}", encryptedMessageString);
            return encryptedMessageString;
        }

        /// <summary>
        /// Encrypting binary data.
        /// </summary>
        /// <param name="data">Binary data to encryption.</param>
        /// <returns>Encrypted data.</returns>
        public byte[] Encrypt(byte[] data)
        {           
            //if (EncryptionKey == null || _SecretKey.Length == 0) throw new ArgumentException("encryptionKey");
            using (var provider = new AesCryptoServiceProvider())
            {
                // Configurating provider.
                provider.Key = (byte[])EncryptionKey;
                provider.Padding = PaddingMode.PKCS7;
                provider.Mode = CipherMode.CBC;

                // Opening stream.
                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        // Adding initialization vector to start of string.
                        ms.Write(provider.IV, 0, 16);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.Flush();//.FlushFinalBlock();
                        }

                        // Return result.
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronous encrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to encryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Encrypted data.</returns>
        public async Task<byte[]> EncryptAsync(byte[] data, System.Threading.CancellationToken cancellationToken)
        {
            if (_SecretKey == null || _SecretKey.Length == 0) throw new ArgumentException("encryptionKey");
            using (var provider = new AesCryptoServiceProvider())
            {
                // Configurating provider.
                provider.Key = _SecretKey;
                provider.Padding = PaddingMode.PKCS7;
                provider.Mode = CipherMode.CBC;

                // Opening stream.
                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        // Adding initialization vector to start of string.
                        ms.Write(provider.IV, 0, 16);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            await cs.WriteAsync(data, 0, data.Length, cancellationToken);
                            cs.FlushFinalBlock();
                        }

                        // Return result.
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// TODO Trying to update key by query.
        /// </summary>
        /// <param name="recivedQuery">Query with shared data.</param>
        /// <returns>Result of updating operation.</returns>
        public bool UpdateWithQuery(UniformQueries.Query recivedQuery)
        {
            throw new NotImplementedException();
        }
    }
}
