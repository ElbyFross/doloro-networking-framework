﻿//Copyright 2019 Volodymyr Podshyvalov
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
using System.IO.Pipes;

namespace PipesProvider.Server.TransmissionControllers
{
    /// <summary>
    /// Controller that provide message's transmisssion from server to client.
    /// </summary>
    public class ServerToClientTransmissionController : BaseServerTransmissionController
    {
        /// <summary>
        /// Query that actualy in processing. 
        /// 
        /// Attention: Value can be changed if some of handlers will call disconecction or transmission error. 
        /// This situation will lead to establishing new connection that lead to changing of this value.
        /// </summary>
        public string ProcessingQuery { get; set; }

        // Set uniform constructor.
        public ServerToClientTransmissionController(
           IAsyncResult connectionMarker,
           System.Action<BaseServerTransmissionController> connectionCallback,
           NamedPipeServerStream pipe,
           string pipeName) : base(
                connectionMarker,
                connectionCallback,
                pipe,
                pipeName)
        { }


        #region Server-Client loops
        /// <summary>
        /// Automaticly create server's pipe that will send message to client.
        /// </summary>
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        public static void ServerLoop(
            string pipeName,
            out string guid,
            Security.SecurityLevel securityLevel)
        {
            // Generate GUID.
            guid = (System.Threading.Thread.CurrentThread.Name + "\\" + pipeName).GetHashCode().ToString();

            // Start loop.
            ServerLoop(guid, pipeName, securityLevel);
        }

        /// <summary>
        /// Automaticly create server's pipe that will send message to client.
        /// </summary>
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        public static void ServerLoop(
            string guid,
            string pipeName,
            Security.SecurityLevel securityLevel)
        {
            // Start loop.
            ServerAPI.ServerLoop<ServerToClientTransmissionController>(
                guid,
                Handlers.DNS.ServerToClientAsync,
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                securityLevel);
        }
        #endregion
    }
}
