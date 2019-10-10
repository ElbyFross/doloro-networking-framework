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
using System.Threading;
using PipesProvider.Server.TransmissionControllers;

namespace UniformServer.Standard
{
    /// <summary>
    /// Server that allow instiniate BaseServer.
    /// Not contain any additive methods.
    /// 
    /// Case of using - simple operations like registing of server for answer.
    /// </summary>
    public class BroadcastingServer : BaseServer
    {
        /// <summary>
        /// Handler that would generate brodcasting message during every new connection.
        /// </summary>
        public BroadcastingServerTransmissionController.MessageHandeler GetMessage;

        /// <summary>
        /// Insiniate broadcasting server.
        /// </summary>
        public BroadcastingServer() : base()
        {

        }


        /// <summary>
        /// Open server with broadcasting chanels using PipesProvider.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        /// <param name="securityLevel">Sequirity level that would be applied to connection.</param>
        /// <param name="getBroadcastingMessageHandler">delegate that will be called to get message for new client.</param>
        /// <param name="chanels">How many many connections would awaiable to this server.
        /// Attention: every chanel is a tread.</param>
        public static void StartBroadcastingViaPP(
            string pipeName,
            PipesProvider.Security.SecurityLevel securityLevel,
            BroadcastingServerTransmissionController.MessageHandeler getBroadcastingMessageHandler,
            int chanels)
        {
            // Open every requested chanel.
            for (int i = 0; i < chanels; i++)
            {
                // Instiniate primitive server to provide loop.
                Standard.BroadcastingServer server = new Standard.BroadcastingServer
                {
                    pipeName = pipeName,
                    securityLevel = securityLevel,
                    // Set handler tha will provide message.
                    GetMessage = getBroadcastingMessageHandler
                };

                // Starting server loop.
                server.StartServerThread(
                    string.Format("BS|{0}|{1}", pipeName, Guid.NewGuid().ToString()),
                    server,
                    ThreadingServerLoop_PP_Broadcast);
            }
        }

        /// <summary>
        /// Main threaded loop that control broadcassting server loop start.
        /// </summary>
        protected static void ThreadingServerLoop_PP_Broadcast(object server)
        {
            if (server is Standard.BroadcastingServer broadcastingServer)
            {
                #region Init
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
                Console.WriteLine("BROADCASTING THREAD STARTED: {0}", Thread.CurrentThread.Name);

                // Name of pipe server that will established.
                // Access to this pipe by clients will be available by this name.
                string serverName = broadcastingServer.thread.Name;
                #endregion

                #region Server establishing
                // Start server loop.
                BroadcastingServerTransmissionController.ServerLoop(
                    serverName,
                    broadcastingServer.pipeName,
                    broadcastingServer.securityLevel,
                    broadcastingServer.GetMessage);
                #endregion
            }
            else
            {
                // Throw error.
                throw new InvalidCastException(
                    "Require Standard.BroadcastingServer server as shared object.");
            }
        }
    }
}
