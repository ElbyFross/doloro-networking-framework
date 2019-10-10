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

namespace PipesProvider.Server.TransmissionControllers
{
    /// <summary>
    /// Controller that provide message's transmisssion from client to server.
    /// </summary>
    public class ClientToServerTransmissionController : BaseServerTransmissionController
    {
        /// <summary>
        /// Delegate that will be called when server will recive query.
        /// ServerTransmissionMeta - meta data of transmission.
        /// Query - shared query.
        /// </summary>
        public Action<BaseServerTransmissionController, UniformQueries.Query> queryHandlerCallback;

        /// <summary>
        /// Instiniate client to server transmission controller.
        /// </summary>
        /// <param name="connectionMarker">Async marker that can be userd to controll of operation.</param>
        /// <param name="connectionCallback">Delegate that would be called when connection will established.</param>
        /// <param name="pipe">Named pipe stream established on the server.</param>
        /// <param name="pipeName">Name of the pipe.</param>
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
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="guid">Generated GUID of this loop.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        /// <param name="securityLevel">Sercruity that would be applied to pipe's server.</param>
        public static void ServerLoop(
            Action<BaseServerTransmissionController, UniformQueries.Query> queryHandlerCallback,
            string pipeName,
            out string guid,
            Security.SecurityLevel securityLevel)
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
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="guid">GUID of this loop.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        /// <param name="securityLevel">Sercruity that would be applied to pipe's server.</param>
        public static void ServerLoop(
            string guid,
            Action<BaseServerTransmissionController, UniformQueries.Query> queryHandlerCallback,
            string pipeName,
            Security.SecurityLevel securityLevel)
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
        /// Automaticly create server's pipe.
        /// Allows to customise GUID.
        /// </summary>
        /// <param name="guid">GUID of this loop.</param>
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        /// <param name="allowedServerInstances">How many server instances can beestablished for that pipe.</param>
        /// <param name="securityLevel">Sercruity that would be applied to pipe's server.</param>
        public static void ServerLoop(
            string guid,
            System.Action<BaseServerTransmissionController, UniformQueries.Query> queryHandlerCallback,
            string pipeName,
            int allowedServerInstances,
            Security.SecurityLevel securityLevel)
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

        /// <summary>
        /// Base server loop that allow full controll of settings.
        /// </summary>
        /// <param name="guid">GUID of this loop.</param>
        /// <param name="queryHandlerCallback">Callback that will be called when server will recive query from clinet.</param>
        /// <param name="pipeName">Name of pipe that will created. Client will access this server using that name.</param>
        /// <param name="pipeDirection">Direction of transmission allowed via this pipe.</param>
        /// <param name="allowedServerInstances">How many server instances can beestablished for that pipe.</param>
        /// <param name="transmissionMode">Define transmission mode that would applied to pipe.</param>
        /// <param name="pipeOptions">Define pipe's option.</param>
        /// <param name="securityLevel">Sercruity that would be applied to pipe's server.</param>
        public static void ServerLoop(
            string guid,
            Action<BaseServerTransmissionController, UniformQueries.Query> queryHandlerCallback,
            string pipeName,
            PipeDirection pipeDirection,
            int allowedServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions pipeOptions,
            Security.SecurityLevel securityLevel)
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
