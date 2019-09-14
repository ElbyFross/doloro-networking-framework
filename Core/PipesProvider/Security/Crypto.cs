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
    /// Class that provide metods for providing server transmission sequrity.
    /// </summary>
    public static class Crypto
    {
        #region Configs
        /// <summary>
        /// Encoder that provide  concertation query from string to byte array.
        /// </summary>
        public static Encoding encoder = Encoding.Default;

        /// <summary>
        /// Padding messages.
        /// </summary>
        public static bool DoOAEPPadding;
        #endregion

        #region Enums
        /// <summary>
        /// Enum  that describe type of SHA hash algorithm.
        /// </summary>
        public enum SHATypes
        {
            /// <summary>
            /// SHA1 encryption algorithm.
            /// </summary>
            SHA1,
            /// <summary>
            /// SHA256 encryption algorithm.
            /// </summary>
            SHA256,
            /// <summary>
            /// SHA384 encryption algorithm.
            /// </summary>
            SHA384,
            /// <summary>
            /// SHA512 encryption algorithm.
            /// </summary>
            SHA512
        }
        #endregion


        #region RSA KEYS
        /// <summary>
        /// Current crypto service provider. Using RSA algortihm with 2048 bit key.
        /// </summary>
        public static RSACryptoServiceProvider CryptoServiceProvider_RSA
        {
            get
            {
                // Create new provider if not found.
                if (_CryptoServiceProvider_RSA == null)
                {
                    #region Init
                    // Create provider.
                    _CryptoServiceProvider_RSA = new RSACryptoServiceProvider();

                    // Set expire time after 24 hours.
                    RSAKeyExpireTime = DateTime.Now.AddDays(1);
                    #endregion

                    #region Auto expire
                    // Compute miliseconds between corent moment and expire date.
                    double timerPeriod = RSAKeyExpireTime.Subtract(DateTime.Now).TotalMilliseconds;
                    RSAProviderExpireTimer = new Timer(timerPeriod);

                    // Create delegate that will be called when timer will be passed.
                    void expireCallback(object sender, ElapsedEventArgs arg)
                    {
                        // unsubscribe.
                        RSAProviderExpireTimer.Elapsed -= expireCallback;

                        // Drop current provider.
                        _CryptoServiceProvider_RSA = null;
                    }
                    // Subscribe exipire handler to timer event.
                    RSAProviderExpireTimer.Elapsed += expireCallback;
                    #endregion
                }

                // Return valid crypto provider.
                return _CryptoServiceProvider_RSA;
            }
        }
        private static RSACryptoServiceProvider _CryptoServiceProvider_RSA;

        private static Timer RSAProviderExpireTimer;

        /// <summary>
        /// Public RSA key that must b used to encrypt of message befor send.
        /// </summary>
        public static RSAParameters PublicKey
        {
            get
            {
                return CryptoServiceProvider_RSA.ExportParameters(false);
            }
        }
        
        /// <summary>
        /// Serialize public key to XML.
        /// </summary>
        /// <returns>Convert pubic key to string format.</returns>
        public static string SerializePublicKey()
        {
            var sw = new StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

            try
            {
                xs.Serialize(sw, PublicKey);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                throw new IOException(ex.Message);
            }
        }

        /// <summary>
        /// Trying to deserialize xml to RSAParameters.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="result"></param>
        /// <returns>Result of operation. Return false if was failed.</returns>
        public static bool TryDeserializeRSAKey(string xml, out RSAParameters result)
        {
            var sr = new StringReader(xml);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

            try
            {
                // Convert xml to object.
                object bufer = xs.Deserialize(sr);
                result = (RSAParameters)bufer;
                return true;
            }
            catch (Exception ex)
            {
                //Log error
                Console.WriteLine("ERROR (PPCr_RSADeser0): {0}", ex.Message);

                // Return fail
                result = new RSAParameters();
                return false;
            }
        }

        /// <summary>
        /// Return private RSA key that can be used to decode message.
        /// </summary>
        public static RSAParameters PrivateKey
        {
            get
            {
                return CryptoServiceProvider_RSA.ExportParameters(true);
            }
        }

        /// <summary>
        /// Time when rsa keys will expired.
        /// </summary>
        public static DateTime RSAKeyExpireTime { get; private set; }
        #endregion

        #region RSA Decryption
        /// <summary>
        /// Decrypt string message that was recived from other source and was encrypted by local public key.
        /// In case of fail will return entry message.
        /// </summary>
        /// <param name="message">Message that will be decrypted.</param>
        /// <returns></returns>
        public static string DecryptString(string message)
        {
            // Conver message to byte array.
            byte[] bytedMessage = encoder.GetBytes(message);
            //byte[] bytedMessage = Convert.FromBase64String(message);

            // Encrypt byte array.
            byte[] encryptedMessage = RSADecrypt(bytedMessage, DoOAEPPadding);

            // Create decrypted string.
            if (encryptedMessage != null)
            {
                // Convert bytes array to string
                string decryptedMessageString = encoder.GetString(encryptedMessage);
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
        /// Decrypt byte array using private key.
        /// </summary>
        /// <param name="DataToDecrypt"></param>
        /// <param name="DoOAEPPadding"></param>
        /// <returns></returns>
        public static byte[] RSADecrypt(byte[] DataToDecrypt, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(PrivateKey);

                    //Decrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
                }
                return decryptedData;
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch //(CryptographicException e)
            {
                //Console.WriteLine("RSA DECRYPTION ERROR:\n{0}", e.ToString());
                return null;
            }
        }
        #endregion

        #region RSA Encryption
        /// <summary>
        /// Encrypt string message and make it ready to trasmition to the server.
        /// </summary>
        /// <param name="message">Message that will be encrypted.</param>
        /// <param name="serverPublicKey">Public encrypt key that was shered by target server.</param>
        /// <returns></returns>
        public static string EncryptString(string message, RSAParameters serverPublicKey)
        {
            // Conver message to byte array.
            byte[] bytedMessage = encoder.GetBytes(message);
            //byte[] bytedMessage = Convert.FromBase64String(message);

            // Encrypt byte array.
            byte[] encryptedMessage = RSAEncrypt(bytedMessage, serverPublicKey, DoOAEPPadding);

            // Create encrypted string.
            string encryptedMessageString = encoder.GetString(encryptedMessage);
            //string encryptedMessageString = Convert.ToBase64String(encryptedMessage);

            //Console.WriteLine("ENCRYPTED TO:\n{0}", encryptedMessageString);
            return encryptedMessageString;
        }

        /// <summary>
        /// Encrypt byte array by public server RSA key.
        /// </summary>
        /// <param name="DataToEncrypt">Data that will be encrypted.</param>
        /// <param name="serverPublicKey">Public encrypt key of target server.</param>
        /// <param name="DoOAEPPadding"></param>
        /// <returns></returns>
        public static byte[] RSAEncrypt(byte[] DataToEncrypt, RSAParameters serverPublicKey, bool DoOAEPPadding)
        {
            try
            {
                byte[] encryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {

                    //Import the RSA Key information. This only needs
                    //toinclude the public key information.
                    RSA.ImportParameters(serverPublicKey);

                    //Encrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    encryptedData = RSA.Encrypt(DataToEncrypt, DoOAEPPadding);
                }
                return encryptedData;
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return null;
            }
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
