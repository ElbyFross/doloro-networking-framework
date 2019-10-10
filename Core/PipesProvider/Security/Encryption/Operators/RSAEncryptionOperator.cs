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
    /// Ecryuption operator that provides API to ecryption by RSA algorithm.
    /// </summary>
    public class RSAEncryptionOperator : IEncryptionOperator
    {
        /// <summary>
        /// Encryption type of that operator.
        /// Define the method of managing that operator.
        /// </summary>
        public EncryptionOperatorType Type { get { return EncryptionOperatorType.Asymmetric; } }

        /// <summary>
        /// Code of that encyptor that allow to detect what encryptor is suitable for data decryption.
        /// </summary>
        public string DecryptionMarker { get; } = "rsa";

        /// <summary>
        /// Encoder that provides concertation query from string to byte array.
        /// </summary>
        public Encoding Encoder { get; set; } = Encoding.Default;

        /// <summary>
        /// Padding messages.
        /// </summary>
        public bool doOAEPPadding = true;

        /// <summary>
        /// Is current encryption provider is valid and can be used in transmission.
        /// </summary>
        public bool IsValid
        {           
            get
            {
                // If crypto provider expired.
                if (_CryptoProvider == null) return false;

                // Check if expired.
                if (ExpiryTime < DateTime.Now) return false;

                return true;
            }
        }

        /// <summary>
        /// Current crypto service provider. Using RSA algortihm with 2048 bit key.
        /// </summary>
        public RSACryptoServiceProvider CryptoProvider
        {
            get
            {
                // Create new provider if not found.
                if (_CryptoProvider == null)
                {
                    #region Init
                    // Create provider.
                    _CryptoProvider = new RSACryptoServiceProvider();

                    // Set expire time after 24 hours.
                    ExpiryTime = DateTime.Now.AddDays(1);
                    #endregion

                    #region Auto expire
                    // Compute miliseconds between corent moment and expire date.
                    double timerPeriod = ExpiryTime.Subtract(DateTime.Now).TotalMilliseconds;
                    RSAProviderExpireTimer = new Timer(timerPeriod);

                    // Create delegate that will be called when timer will be passed.
                    void expireCallback(object sender, ElapsedEventArgs arg)
                    {
                        // unsubscribe.
                        RSAProviderExpireTimer.Elapsed -= expireCallback;

                        // Drop current provider.
                        _CryptoProvider = null;
                    }
                    // Subscribe exipire handler to timer event.
                    RSAProviderExpireTimer.Elapsed += expireCallback;
                    #endregion
                }

                // Return valid crypto provider.
                return _CryptoProvider;
            }
        }

        /// <summary>
        /// Bufer that contains current instiniated crypto service provider.
        /// </summary>
        private RSACryptoServiceProvider _CryptoProvider;

        /// <summary>
        /// Timer that would expire RSA keys to prevent coruption of security.
        /// </summary>
        private Timer RSAProviderExpireTimer;

        /// <summary>
        /// Time in minutes that during current keys is valid.
        /// Less or equal zero mark session as endless. In this case key wouldn't updated.
        /// </summary>
        public int SessionTime { get; set; } = 1440; // 24 hours by default.

        /// <summary>
        /// Time when current keys' session would by expired.
        /// </summary>
        public DateTime ExpiryTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Public keys in string format allowed to sharing in message format.
        /// </summary>
        public byte[] SharableData
        {
            get
            {
                return UniformDataOperator.Binary.BinaryHandler.ToByteArray(EncryptionKey);

                // Deprecated xml data.
                //var sw = new StringWriter();
                //var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                //xs.Serialize(sw, EncryptionKey);
                //return sw.ToString();
            }
            set
            {
                try
                {
                    var ekBufer = UniformDataOperator.Binary.BinaryHandler.FromByteArray<RSAParameters>(value);
                    EncryptionKey = ekBufer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine
                        ("RSA EO ERROR: Invalid sharable data. Leaved the previous " + 
                        "value of Encryption key. Details:\n" + ex.Message);
                }

                // Deprecated xml data.
                //var sr = new StringReader(value);
                //var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                //// Convert xml to object.
                //object bufer = xs.Deserialize(sr);
                //EncryptionKey = bufer;
            }
        }

        /// <summary>
        /// Public RSA key that must be used to encrypt of message before transmission.
        /// Set always cause NotSupportedException.
        /// </summary>
        public object EncryptionKey
        {
            get
            {
                return CryptoProvider.ExportParameters(false);
            }
            set
            {
                CryptoProvider.ImportParameters((RSAParameters)value);
            }
        }

        /// <summary>
        /// Return private RSA key that can be used to decode message.
        /// Set always cause NotSupportedException.
        /// </summary>
        public object DecryptionKey
        {
            get
            {
                return CryptoProvider.ExportParameters(true);
            }
            set => throw new NotSupportedException();
        }
        

        /// <summary>
        /// Decrypt string message that was recived from other source and was encrypted by local public key.
        /// In case of fail will return entry message.
        /// </summary>
        /// <param name="message">Message that will be decrypted.</param>
        /// <returns></returns>
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
        /// Decrypt byte array using private key.
        /// </summary>
        /// <param name="DataToDecrypt"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] DataToDecrypt)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters((RSAParameters)DecryptionKey);

                    //Decrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    decryptedData = RSA.Decrypt(DataToDecrypt, doOAEPPadding);
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

        /// <summary>
        /// Asynchronous decrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to decryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Decrypted data.</returns>
        public async Task<byte[]> DecryptAsync(byte[] data, System.Threading.CancellationToken cancellationToken)
        {
            byte[] decryptedData = null;
            await Task.Run(delegate () 
            {
                decryptedData = Decrypt(data);
            }, cancellationToken);
            return decryptedData;
        }

        /// <summary>
        /// Encrypt string message and make it ready to trasmition to the server.
        /// </summary>
        /// <param name="message">Message that will be encrypted.</param>
        /// <param name="serverPublicKey">Public encrypt key that was shered by target server.</param>
        /// <returns></returns>
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
        /// Encrypt byte array by public server RSA key.
        /// </summary>
        /// <param name="DataToEncrypt">Data that will be encrypted.</param>
        /// <param name="serverPublicKey">Public encrypt key of target server.</param>
        /// <param name="DoOAEPPadding"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] DataToEncrypt)
        {
            try
            {
                byte[] encryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {

                    //Import the RSA Key information. This only needs
                    //toinclude the public key information.
                    RSA.ImportParameters((RSAParameters)EncryptionKey);

                    //Encrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    encryptedData = RSA.Encrypt(DataToEncrypt, doOAEPPadding);
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

        /// <summary>
        /// Asynchronous encrypt binary data.
        /// </summary>
        /// <param name="data">Binary data to encryption.</param>
        /// <param name="cancellationToken">Token that can can be used for termination of operation.</param>
        /// <returns>Encrypted data.</returns>
        public async Task<byte[]> EncryptAsync(byte[] data, System.Threading.CancellationToken cancellationToken)
        {
            byte[] encryptedData = null;
            await Task.Run(delegate() 
            {
                encryptedData = Encrypt(data);
            }, cancellationToken);
            return encryptedData;
        }

        /// <summary>
        /// Try to update Public RSA key by query recived from server as reply to GET PUBLICKEY query.
        /// </summary>
        /// <param name="recivedQuery">Query with shared data.</param>
        /// <returns>Result of updating operation.</returns>
        public bool UpdateWithQuery(UniformQueries.Query recivedQuery)
        {
            #region Query processing
            // Get RSA public key
            if (!recivedQuery.TryGetParamValue("pk", out UniformQueries.QueryPart publicKey))
            {
                Console.WriteLine("ERROR (BCRT1): Incorrect answer format. Require \"pk\" propety.");
                return false;
            }

            // Get expire param
            if (!recivedQuery.TryGetParamValue("expire", out UniformQueries.QueryPart expireDate))
            {

                Console.WriteLine("ERROR (BCRT1): Incorrect answer format. Require \"expire\" propety.");
                return false;
            }

            RSAParameters keyBufer;
            DateTime expireTimeBufer;

            // Deserialize key.
            try
            {
                // Creating bufer operator to operate with sharable data.
                keyBufer = (RSAParameters)(new RSAEncryptionOperator
                {
                    // Apply recived binary data as sharable value.
                    SharableData = publicKey.propertyValue
                }).EncryptionKey; // Getting deserialized key.
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR(BCRT2): {0}", ex.Message);
                return false;
            }

            // Parse expire time.
            try
            {
                expireTimeBufer = DateTime.FromBinary(UniformDataOperator.Binary.BinaryHandler.FromByteArray<long>(expireDate.propertyValue));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR(BCRT3): {0}", ex.Message);
                return false;
            }
            #endregion

            // Set pufers to block if operation completed.
            EncryptionKey = keyBufer;
            ExpiryTime = expireTimeBufer;
            
            return true;
        }
    }
}
