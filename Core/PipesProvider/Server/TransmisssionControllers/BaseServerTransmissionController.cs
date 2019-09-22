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
using PipesProvider.Security.Encryption;
using System.Collections;
using System.IO.Pipes;

namespace PipesProvider.Server.TransmissionControllers
{
    /// <summary>
    /// Container that contains meta data about server instance.
    /// </summary>
    public class BaseServerTransmissionController
    {
        #region Fields & properties
        /// <summary>
        /// Object that provide access to async connection.
        /// </summary>
        public IAsyncResult connectionMarker;

        /// <summary>
        /// Delegate that will be called when connection will be established.
        /// ServerTransmissionMeta - meta data of transmission.
        /// </summary>
        public System.Action<BaseServerTransmissionController> connectionCallback;
        
        /// <summary>
        /// Reference to created pipe.
        /// </summary>
        public NamedPipeServerStream pipeServer;

        /// <summary>
        /// Name of this connection.
        /// </summary>
        public string pipeName;

        /// <summary>
        /// Marker that show does current transmission is relevant.
        /// When it'll become true this pipe connection will be desconected.
        /// </summary>
        public bool Expired { get; protected set; }

        /// <summary>
        /// Marker that show does this transmition stoped.
        /// </summary>
        public bool Stoped { get; protected set; }

        /// <summary>
        /// Asymmedtric key that would be used to encrypting short message before sending to that server.
        /// </summary>
        public static IEncryptionOperator AsymmetricKey { get; set; } =  new RSAEncryptionOperator();

        /// <summary>
        /// Hashtable that contains generated symmetric keys relative to client's guid.
        /// 
        /// Key - string guid.
        /// Value - Security.Encryption.IEncryptionOperator security descriptor 
        /// that contains generated keys and control expiry operation.
        /// </summary>
        protected static readonly Hashtable symmetricKeys = new Hashtable();
        #endregion


        #region Constructors
        /// <summary>
        /// instiniate default server trnasmission controller.
        /// </summary>
        public BaseServerTransmissionController() { }

        /// <summary>
        /// Instiniate base transmission controller.
        /// </summary>
        /// <param name="connectionMarker">Async marker that can be userd to controll of operation.</param>
        /// <param name="connectionCallback">Delegate that would be called when connection will established.</param>
        /// <param name="pipe">Named pipe stream established on the server.</param>
        /// <param name="pipeName">Name of the pipe.</param>
        public BaseServerTransmissionController(
            IAsyncResult connectionMarker, 
            System.Action<BaseServerTransmissionController> connectionCallback,
            NamedPipeServerStream pipe, string pipeName)
        {
            this.connectionMarker = connectionMarker;
            this.connectionCallback = connectionCallback;
            this.pipeServer = pipe;
            this.pipeName = pipeName;
            Expired = false;
            Stoped = false;
        }

        /// <summary>
        /// Return instance that not contain initialized fields.
        /// </summary>
        public static BaseServerTransmissionController None
        {
            get { return new BaseServerTransmissionController(); }
        }
        #endregion

        #region API
        /// <summary>
        /// Maeking transmission as expired. Line will be remaked.
        /// </summary>
        public void SetExpired()
        {
            Expired = true;

            Console.WriteLine("{0}: PIPE SERVER MANUALY EXPIRED", pipeName);
        }

        /// <summary>
        /// Marking transmission as expired and stoped for full exclusion 
        /// from automatic server operations.
        /// </summary>
        public void SetStoped()
        {
            Stoped = true;
            Expired = true;

            Console.WriteLine("{0}: PIPE SERVER MANUALY STOPED", pipeName);
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
            if(symmetricKeys[guid] is IEncryptionOperator eo)
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
        #endregion
    }
}
