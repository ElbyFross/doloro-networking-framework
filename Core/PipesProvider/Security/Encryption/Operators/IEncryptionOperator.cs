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
using System.Threading;
using System.Threading.Tasks;

namespace PipesProvider.Security.Encryption.Operators
{
    /// <summary>
    /// Interface that allow to implement uniformed data encryptor 
    /// that would be used during transmission security operations.
    /// </summary>
    public interface IEncryptionOperator
    {
        /// <summary>
        /// Encoder that provides concertation query from string to byte array.
        /// </summary>
        Encoding Encoder { get; set; }

        /// <summary>
        /// Is current encryption provider is valid and can be used in transmission.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Time in minutes that during current keys is valid.
        /// Less or equal zero mark session as endless. In this case key wouldn't updated.
        /// </summary>
        int SessionTime { get; set; }

        /// <summary>
        /// Time when current keys' session would by expired.
        /// Available only if server informs about.
        /// </summary>
        DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Object that contains suitable data that can be used to encryption\decryption of data.
        /// </summary>
        object EncryptionKey { get; set; }

        /// <summary>
        /// Object that contains data suitable for encryption of received transmission data.
        /// </summary>
        object DecryptionKey { get; set; }

        /// <summary>
        /// Public keys in binary format allowed to sharing in message format.
        /// </summary>
        byte[] SharableData { get; set; }

        /// <summary>
        /// Encrypting string message.
        /// </summary>
        /// <param name="message">Messege to encryption.</param>
        /// <returns>Encrypted message</returns>
        string Encrypt(string message);

        /// <summary>
        /// Encrypting binary data.
        /// </summary>
        /// <param name="data">Binary data to encryption.</param>
        /// <returns>Encrypted data.</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Asynchronous encrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to encryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Encrypted data.</returns>
        Task<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken);
        
        /// <summary>
        /// Decrypting string message.
        /// </summary>
        /// <param name="message">Messege to decryption.</param>
        /// <returns>Decrypted message</returns>
        string Decrypt(string message);

        /// <summary>
        /// Decrypting binary data.
        /// </summary>
        /// <param name="data">Binary data to decryption.</param>
        /// <returns>Decrypted data.</returns>
        byte[] Decrypt(byte[] data);

        /// <summary>
        /// Asynchronous decrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to decryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Decrypted data.</returns>
        Task<byte[]> DecryptAsync(byte[] data, CancellationToken cancellationToken);

        /// <summary>
        /// Trying to update key by query.
        /// </summary>
        /// <param name="recivedQuery">Query with shared data.</param>
        /// <returns>Result of updating operation.</returns>
        bool UpdateWithQuery(UniformQueries.Query recivedQuery);
    }
}
