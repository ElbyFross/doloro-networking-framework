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
    /// Transmission controller that situable for broadcasting.
    /// Every connection will call Message handler that can share scripted or constant data.
    /// </summary>
    public class BroadcastingServerTransmissionController : BaseServerTransmissionController
    {
        #region Properties
        /// <summary>
        /// Handler that contain delegate that generate message for every broadcasting session.
        /// </summary>
        public MessageHandeler GetMessage { get; set; }
        public delegate string MessageHandeler(BroadcastingServerTransmissionController transmissionController);
        #endregion

        #region Constructors
        // Set uniform constructor.
        public BroadcastingServerTransmissionController(
           IAsyncResult connectionMarker,
           System.Action<BaseServerTransmissionController> connectionCallback,
           NamedPipeServerStream pipe, 
           string pipeName) : base(
                connectionMarker, 
                connectionCallback,
                pipe, 
                pipeName) { }
        #endregion

        #region Server loops
        /// <summary>
        /// Start server loop that provide server pipe.
        /// </summary>
        /// <param name="guid">Id signed to connection. Will generated authomaticly and returned to </param>
        /// <param name="pipeName">Name of the pipe that will be started.</param>
        /// <param name="securityLevel">Pipes security levels that will be applied to pipe.</param>
        /// <param name="getMessageHanler">Handler that generate brodcasting message for every connected client.</param>
        public static void ServerLoop(
           out string guid,
           string pipeName,
           Security.SecurityLevel securityLevel,
            BroadcastingServerTransmissionController.MessageHandeler getMessageHanler)
        {
            // Generate GUID.
            guid = (System.Threading.Thread.CurrentThread.Name + "\\" + pipeName).GetHashCode().ToString();

            // Start loop.
            ServerLoop(guid, pipeName, securityLevel, getMessageHanler);
        }

        /// <summary>
        /// 
        /// Start server loop that provide server pipe.
        /// </summary>
        /// <param name="guid">Id signed to connection.</param>
        /// <param name="pipeName">Name of the pipe that will be started.</param>
        /// <param name="securityLevel">Pipes security levels that will be applied to pipe.</param>
        /// <param name="getMessageHanler">Handler that generate brodcasting message for every connected client.</param>
        public static void ServerLoop(
            string guid,
            string pipeName,
            Security.SecurityLevel securityLevel,
            BroadcastingServerTransmissionController.MessageHandeler getMessageHanler)
        {
            // Start loop.
            ServerAPI.ServerLoop<BroadcastingServerTransmissionController>(
                guid,
                Handlers.DNS.ServerBroadcasting,
                pipeName,
                PipeDirection.InOut,
                System.IO.Pipes.NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                securityLevel,
                // Initialise broadcasting delegate.
                (BaseServerTransmissionController tc) =>
                    {
                        if (tc is BroadcastingServerTransmissionController bstc)
                        {
                            bstc.GetMessage = getMessageHanler;
                        }
                        else
                        {
                            // Stop server
                            tc.SetStoped();

                            // Log error
                            Console.WriteLine("SERVER ERROR (BSS_TC 10): Created controler can't be casted to BroadcastingServerTransmissionController");
                        }
                    }
                );
        }
        #endregion
    }
}
