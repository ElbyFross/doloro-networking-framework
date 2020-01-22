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
    /// Part of class that provides methods those can be started as server thread.
    /// </summary>
    public partial class BaseServer
    {
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
            string serverName = ((BaseServer)server).ServerThread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            ServerToClientTransmissionController.ServerLoop(
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
            string serverName = ((BaseServer)server).ServerThread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            ClientToServerTransmissionController.ServerLoop(
                serverName,
                PipesProvider.Handlers.Queries.ProcessingAsync,
                ((BaseServer)server).pipeName,
                ((BaseServer)server).securityLevel);
            #endregion
        }
    }
}
