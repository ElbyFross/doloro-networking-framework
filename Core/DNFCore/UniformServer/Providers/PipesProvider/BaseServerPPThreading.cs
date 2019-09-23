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
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;

namespace UniformServer
{
    /// <summary>
    /// Part of class that provide methods can be started as thread for server init;
    /// </summary>
    public abstract partial class BaseServer
    {
        #region Events
        /// <summary>
        /// Event will be called when system will request a thread termination.
        /// Argument - index of thread.
        /// </summary>
        public static event System.Action<int> ThreadTerminateRequest;

        /// <summary>
        /// Event that will be called when seystem will require a thread start.
        /// Argument - index of thread.
        /// </summary>
        public static event System.Action<int> ThreadStartRequest;
        #endregion


        /// <summary>
        ///  Main loop that control monitor thread.
        /// </summary>
        protected static void ThreadingServerLoop_PP_Output(object server)
        {
            #region Init
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("OUTPUT THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Name of pipe server that will established.
            // Access to this pipe by clients will be available by this name.
            string serverName = ((BaseServer)server).thread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            PipesProvider.Server.TransmissionControllers.ServerToClientTransmissionController.ServerLoop(
                serverName,
                ((BaseServer)server).pipeName,
                ((BaseServer)server).securityLevel);
            #endregion
        }

        /// <summary>
        ///  Main loop that control pipe chanel that will recive clients.
        /// </summary>
        protected static void ThreadingServerLoop_PP_Input(object server)
        {
            #region Init
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("INPUT THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Name of pipe server that will established.
            // Access to this pipe by clients will be available by this name.
            string serverName = ((BaseServer)server).thread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            PipesProvider.Server.TransmissionControllers.ClientToServerTransmissionController.ServerLoop(
                serverName,
                PipesProvider.Handlers.Queries.ProcessingAsync,
                ((BaseServer)server).pipeName,
                ((BaseServer)server).securityLevel);
            #endregion
        }
    }
}
