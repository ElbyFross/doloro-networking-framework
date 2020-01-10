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
    /// A controller that contains metadata about server instance 
    /// and provides an API to manage it.
    /// </summary>
    public class BaseServerTransmissionController
    {
        #region Fields & properties
        /// <summary>
        /// Object that provide access to async connection.
        /// </summary>
        public IAsyncResult connectionMarker;

        /// <summary>
        /// Marker that autorize new connection search.
        /// </summary>
        public bool NewConnectionSearchAllowed { get; set; } = true;

        /// <summary>
        /// A delegate that will be called when connection will be established.
        /// ServerTransmissionMeta - meta data of transmission.
        /// </summary>
        public Action<BaseServerTransmissionController> connectionCallback;
        
        /// <summary>
        /// A reference to created server pipe.
        /// </summary>
        public NamedPipeServerStream PipeServer { get; set; }

        /// <summary>
        /// Name of the server pipe.
        /// </summary>
        public string PipeName { get; protected set; }

        /// <summary>
        /// Marker that shows is the transmission is relevant.
        /// When it'll become true the pipe connection will be desconected.
        /// </summary>
        public bool Expired { get; protected set; }

        /// <summary>
        /// Marker that shows is this transmition stoped.
        /// </summary>
        public bool Stoped { get; protected set; }
        #endregion


        #region Constructors
        /// <summary>
        /// instiniate default server trnasmission controller.
        /// </summary>
        public BaseServerTransmissionController() { }

        /// <summary>
        /// Instiniates a base transmission controller.
        /// </summary>
        /// <param name="connectionMarker">An async marker that can be used to control of operation.</param>
        /// <param name="connectionCallback">A delegate that will invoked when connection became established.</param>
        /// <param name="pipe">A named pipe stream established on the server.</param>
        /// <param name="pipeName">A pipe's name.</param>
        public BaseServerTransmissionController(
            IAsyncResult connectionMarker,
            Action<BaseServerTransmissionController> connectionCallback,
            NamedPipeServerStream pipe, 
            string pipeName)
        {
            this.connectionMarker = connectionMarker;
            this.connectionCallback = connectionCallback;
            this.PipeServer = pipe;
            this.PipeName = pipeName;
            Expired = false;
            Stoped = false;
        }

        /// <summary>
        /// Returns an instance that not contains initialized fields.
        /// </summary>
        public static BaseServerTransmissionController None
        {
            get { return new BaseServerTransmissionController(); }
        }
        #endregion

        #region API
        /// <summary>
        /// Marks the transmission like expired. The line will be remaked.
        /// </summary>
        public void SetExpired()
        {
            Expired = true;

            Console.WriteLine("{0}: PIPE SERVER MANUALY EXPIRED", PipeName);
        }

        /// <summary>
        /// Marks the transmission as expired and stoped for full exclusion 
        /// from automatic server operations.
        /// </summary>
        public void SetStoped()
        {
            Stoped = true;
            Expired = true;

            Console.WriteLine("{0}: PIPE SERVER MANUALY STOPED", PipeName);
        }
        #endregion
    }
}
