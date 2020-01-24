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
using System.IO.Pipes;
using UniformQueries;
using PipesProvider.Security;

namespace PipesProvider.Server.TransmissionControllers
{
    /// <summary>
    /// A controller that provides data transmission from a server to a client.
    /// </summary>
    public class ServerToClientTransmissionController : BaseServerTransmissionController
    {
        /// <summary>
        /// A query in processing. 
        /// 
        /// Attention: A value can be changed if some of handlers will call disconnection or transmission error.
        /// This situation will lead to establishing a new connection that leads to changing of this value.
        /// </summary>
        public Query ProcessingQuery { get; set; }

        /// <summary>
        /// Instiniates a server to client transmission controller.
        /// </summary>
        /// <param name="connectionMarker">
        /// An async marker that can be used to operation control.
        /// </param>
        /// <param name="connectionCallback">
        /// A delegate that will be invoked when connection will established.
        /// </param>
        /// <param name="pipe">
        /// A named pipe stream established on the server.
        /// </param>
        /// <param name="pipeName">
        /// A name of the pipe.
        /// </param>
        public ServerToClientTransmissionController(
           IAsyncResult connectionMarker,
           Action<BaseServerTransmissionController> connectionCallback,
           NamedPipeServerStream pipe,
           string pipeName) : base(
                connectionMarker,
                connectionCallback,
                pipe,
                pipeName)
        { }


        #region Server-Client loops
        /// <summary>
        /// Automatically creates a server pipe that will send a message to a client.
        /// </summary>
        /// <param name="pipeName">
        /// A name of pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="guid">
        /// Generated unique GUID of this loop.
        /// </param>
        /// <param name="securityLevel">
        /// A security level that will be applied to the server's pipe.
        /// </param>
        public static void ServerLoop(
            string pipeName,
            out string guid,
            SecurityLevel securityLevel)
        {
            // Generates a GUID.
            guid = (System.Threading.Thread.CurrentThread.Name + "\\" + pipeName).
                GetHashCode().ToString();

            // Starts a loop.
            ServerLoop(guid, pipeName, securityLevel);
        }

        /// <summary>
        /// Automatically creates a server pipe that will send a message to a client.
        /// </summary>
        /// <param name="pipeName">
        /// A name of pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="guid">
        /// A GUID of this loop.
        /// </param>
        /// <param name="securityLevel">
        /// A security level that will be applied to the server's pipe.
        /// </param>
        public static void ServerLoop(
            string guid,
            string pipeName,
            SecurityLevel securityLevel)
        {
            // Starts a loop.
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
