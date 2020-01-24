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
    /// A controller that provides data transmission from a client to a server.
    /// </summary>
    public class ClientToServerTransmissionController : BaseServerTransmissionController
    {
        /// <summary>
        /// A delegate that will invoked when a server recived a query.
        /// ServerTransmissionMeta - a metadata of the transmission.
        /// Query - a received query.
        /// </summary>
        public Action<BaseServerTransmissionController, Query> queryHandlerCallback;

        /// <summary>
        /// Instiniates a client to server transmission controller.
        /// </summary>
        /// <param name="connectionMarker">
        /// An async marker that can be used to control of the operation.
        /// </param>
        /// <param name="connectionCallback">
        /// A delegate that will invoked when connection will established.
        /// </param>
        /// <param name="pipe">
        /// A named pipe stream established on the server.
        /// </param>
        /// <param name="pipeName">
        /// A name of the pipe.
        /// </param>
        public ClientToServerTransmissionController(
           IAsyncResult connectionMarker,
           Action<BaseServerTransmissionController> connectionCallback,
           NamedPipeServerStream pipe,
           string pipeName) : base(
                connectionMarker,
                connectionCallback,
                pipe,
                pipeName)
        { }


        #region Client-Server loops
        /// <summary>
        /// Automaticly create server's pipe that will recive queries from clients.
        /// </summary>
        /// <param name="queryHandlerCallback">
        /// A callback that will invoked when server will recive a query from a client.
        /// </param>
        /// <param name="guid">
        /// Generated GUID of this loop.
        /// </param>
        /// <param name="pipeName">
        /// Name of pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="securityLevel">
        /// Sercruity that would be applied to pipe's server.
        /// </param>
        public static void ServerLoop(
            Action<BaseServerTransmissionController, Query> queryHandlerCallback,
            string pipeName,
            out string guid,
            SecurityLevel securityLevel)
        {
            // Generate GUID.
            guid = (System.Threading.Thread.CurrentThread.Name + "\\" + pipeName).GetHashCode().ToString();

            // Call loop
            ServerLoop(
                guid,
                queryHandlerCallback,
                pipeName,
                securityLevel);
        }

        /// <summary>
        /// Automaticly create server's pipe.
        /// Allows to customise GUID.
        /// </summary>
        /// <param name="queryHandlerCallback">
        /// A callback that will invoked when server will recive a query from a client.
        /// </param>
        /// <param name="guid">
        /// An unique GUID of this loop.
        /// </param>
        /// <param name="pipeName">
        /// A name of a pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="securityLevel"
        /// >A security level that will be applied to the server pipe.
        /// </param>
        public static void ServerLoop(
            string guid,
            Action<BaseServerTransmissionController, Query> queryHandlerCallback,
            string pipeName,
            SecurityLevel securityLevel)
        {
            // Start loop
            ServerLoop(
                guid,
                queryHandlerCallback,
                pipeName,
                NamedPipeServerStream.MaxAllowedServerInstances,
                securityLevel);
        }

        /// <summary>
        /// Automaticly creates a server pipe.
        /// Allows to customise GUID.
        /// </summary>
        /// <param name="guid">
        /// An unique GUID of the loop.
        /// </param>
        /// <param name="queryHandlerCallback">
        /// A callback that will invoked when server will recive a query from a client.
        /// </param>
        /// <param name="pipeName">
        /// A name of a pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="allowedServerInstances">
        /// How many server instances can be established for that pipe.
        /// </param>
        /// <param name="securityLevel">
        /// A security level that will be applied to the server pipe.
        /// </param>
        public static void ServerLoop(
            string guid,
            Action<BaseServerTransmissionController, Query> queryHandlerCallback,
            string pipeName,
            int allowedServerInstances,
            SecurityLevel securityLevel)
        {
            ServerLoop(
                guid,
                queryHandlerCallback,
                pipeName,
                PipeDirection.InOut,
                allowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                securityLevel);
        }

        /// <summary>
        /// Automaticly create server's pipe.
        /// Allows to customise GUID.
        /// </summary>
        /// <param name="guid">GUID of this loop.</param>
        /// <param name="queryHandlerCallback">
        /// A callback that will invoked when server will recive a query from a client.
        /// </param>
        /// <param name="pipeName">
        /// A name of a pipe that will created. Client will access this server using that name.
        /// </param>
        /// <param name="pipeDirection">
        /// Direction of transmission allowed via the pipe.
        /// </param>
        /// <param name="allowedServerInstances">
        /// How many server instances can be established for that pipe.
        /// </param>
        /// <param name="transmissionMode">
        /// Defines a transmission mode that will applied to the pipe.
        /// </param>
        /// <param name="pipeOptions">
        /// Defines pipe's options.
        /// </param>
        /// <param name="securityLevel">
        /// A security level that will be applied to the server pipe.
        /// </param>
        public static void ServerLoop(
            string guid,
            Action<BaseServerTransmissionController, Query> queryHandlerCallback,
            string pipeName,
            PipeDirection pipeDirection,
            int allowedServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions pipeOptions,
            SecurityLevel securityLevel)
        {
            ServerAPI.ServerLoop<ClientToServerTransmissionController>(
                guid,
                Handlers.DNS.ClientToServerAsync,
                pipeName,
                pipeDirection,
                allowedServerInstances,
                transmissionMode,
                pipeOptions,
                securityLevel,
                // Set query handler callback
                (BaseServerTransmissionController tc) =>
                    {
                        if (tc is ClientToServerTransmissionController csts)
                        {
                            csts.queryHandlerCallback = queryHandlerCallback;
                        }
                        else
                        {
                            // Stop server
                            tc.SetStoped();

                            // Log error
                            Console.WriteLine("SERVER ERROR (CtS_TC 10): Created controler can't be casted to ClientToServerTransmissionController");
                        }
                    }
                );
        }
        #endregion
    }
}
