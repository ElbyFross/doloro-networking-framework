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
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace UniformServer
{
    /// <summary>
    /// Class that provide base server features and envirounment static API.
    /// </summary>
    public partial class BaseServer
    {        
        /// <summary>
        /// A current started server thread. Null if not started.
        /// </summary>
        public Thread ServerThread
        {
            get { return thread; }
            protected set { thread = value; }
        }

        /// <summary>
        /// Name that will be applied to the pipe.
        /// </summary>
        public string pipeName;
        
        /// <summary>
        /// Reference to thread that host this server.
        /// </summary>
        private Thread thread;

        #region Multithreading
        /// <summary>
        /// Method that starting server thread.
        /// </summary>
        /// <param name="threadName">Name that would be applied to thread.</param>
        /// <param name="server">A server instance that will be stared to processing</param>
        /// <param name="serverLoop">Loop delegate with sharable param.</param>
        /// <returns>Established thread.</returns>
        protected Thread StartServerThread(
            string threadName, 
            BaseServer server, 
            ParameterizedThreadStart serverLoop)
        {
            // Initialize queries monitor thread.
            ServerThread = new Thread(serverLoop)
            {
                Name = threadName,
                Priority = ThreadPriority.BelowNormal
            };

            // Start thread
            ServerThread.Start(server);

            // Let it proceed first run.
            Thread.Sleep(ServerAppConfigurator.PreferedThreadsSleepTime);

            // Started thread.
            return ServerThread;
        }
        #endregion
    }
}