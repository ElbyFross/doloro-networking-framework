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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using PipesProvider.Security;
using PipesProvider.Server;
using PipesProvider.Client;
using PipesProvider.Networking.Routing;

namespace SessionProvider
{
    /// <summary>
    /// Server tasks:
    /// - Provide session token to clients.
    /// - User data managment.
    /// - Control rights.
    /// </summary>
    class Server : UniformServer.BaseServer
    {
        /// <summary>
        /// Routing table that contain instructions to access reletive servers
        /// that need to be informed about token events.
        /// 
        /// Before sharing query still will check is the query stituable for that routing instruction.
        /// If you no need any filtring then just leave query patterns empty.
        /// </summary>
        public static RoutingTable relatedServers;

        public static bool UsersLoaded
        {
            get;
            private set;
        }

        static void Main(string[] args)
        {
            #region Detect processes conflicts
            // Get GUID of this assebly.
            string guid = Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly()).ToString();

            // Create Mutex for this app instance.
            mutexObj = new Mutex(true, guid, out bool newApp);

            // Check does this instance a new single app, or same app already runned.
            if (!newApp)
            {
                // Log error.
                Console.WriteLine("\"THB Data Server\" already started. Application not allow multiple instances at single moment.\nGUID: " + guid);
                // Wait a time until exit.
                Thread.Sleep(2000);
                return;
            }
            #endregion

            #region Set default data \ load DLLs \ appling arguments
            // Set default thread count. Can be changed via args or command.
            threadsCount = Environment.ProcessorCount;
            longTermServerThreads = new Server[threadsCount];

            // React on uniform arguments.
            ArgsReactor(args);

            // Check direcroties
            LoadAssemblies(AppDomain.CurrentDomain.BaseDirectory + "libs\\");
            #endregion

            #region Initialize authority controller
            // Subscribe to events.
            AuthorityController.Session.InformateRelatedServers += InformateRelatedServers;

            // Load users.
            AuthorityController.API.Users.DirectoryLoadingFinished += Users_DirectoryLoadingUnlocked;
            AuthorityController.API.Users.LoadProfilesAsync(AuthorityController.Data.Config.Active.UsersStorageDirectory);
            #endregion

            #region Guest tokens broadcasting
            // Start broadcasting server that would share guest tokens.
            UniformServer.BaseServer.StartBroadcastingViaPP(
                "guests",
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);
            #endregion

            /// Show help.
            UniformServer.Commands.BaseCommands("help");

            #region Main loop
            // Main loop that will provide server services until application close.
            while (!appTerminated)
            {
                // Check input
                if (Console.KeyAvailable)
                {
                    // Log responce.
                    Console.Write("\nEnter command: ");

                    // Read command.
                    string command = Console.ReadLine();

                    // Processing of entered command.
                    UniformServer.Commands.BaseCommands(command);
                }
                Thread.Sleep(threadSleepTime);
            }
            #endregion

            #region Finalize
            Console.WriteLine();

            // Stop started servers.
            ServerAPI.StopAllServers();

            // Unsubscribe from events
            AuthorityController.Session.InformateRelatedServers -= InformateRelatedServers;
            AuthorityController.API.Users.DirectoryLoadingFinished -= Users_DirectoryLoadingUnlocked;

            // Whait until close.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            #endregion
        }

        private static void Users_DirectoryLoadingUnlocked(string unlockedDirectory, int sucess, int failed)
        {
            // If users storage loaded.
            if (unlockedDirectory.Equals(AuthorityController.Data.Config.Active.UsersStorageDirectory))
            {
                // Mark users like unlocked.
                UsersLoaded = true;

                // TODO Check super admin existing.
            }
        }


        /// <summary>
        /// Transmit information to every related server that suitable for query format.
        /// </summary>
        /// <param name="message"></param>
        private static void InformateRelatedServers(string message)
        {
            // Inform relative servers.
            if (relatedServers != null)
            {
                // Check every instruction.
                for (int i = 0; i < relatedServers.intructions.Count; i++)
                {
                    // Get instruction.
                    Instruction instruction = relatedServers.intructions[i];

                    // Does instruction situable to query.
                    if (!instruction.IsRoutingTarget(message))
                    {
                        // Skip if not.
                        continue;
                    }

                    // Open transmission line to server.
                    UniformClient.BaseClient.OpenOutTransmissionLineViaPP(instruction.routingIP, instruction.pipeName).
                        EnqueueQuery(message).                  // Add query to queue.
                        SetInstructionAsKey(ref instruction).   // Apply encryption if requested.
                        TryLogonAs(instruction.logonConfig);    // Profide logon data to access remote machine.
                }
            }
        }
    }
}
