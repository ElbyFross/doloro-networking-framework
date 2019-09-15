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
using PipesProvider.Server;
using PipesProvider.Networking.Routing;
using AuthorityController.Data.Application;

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
            longTermServerThreads = new UniformServer.BaseServer[threadsCount];

            // React on uniform arguments.
            ArgsReactor(args);

            // Check direcroties
            LoadAssemblies(AppDomain.CurrentDomain.BaseDirectory + "libs\\");
            #endregion

            #region Initialize authority controller
            // Subscribe to events.
            //AuthorityController.Session.InformateRelatedServers += InformateRelatedServers;

            // Load users.
            AuthorityController.API.LocalUsers.DirectoryLoadingFinished += Users_DirectoryLoadingUnlocked;
            AuthorityController.API.LocalUsers.LoadProfilesAsync(Config.Active.UsersStorageDirectory);
            #endregion

            #region Loaded query handler processors.
            /// Draw line
            ConsoleDraw.Primitives.DrawSpacedLine();
            // Initialize Queue monitor.
            try
            {
                _ = UniformQueries.API.QueryHandlers;
            }
            catch (Exception ex)
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Console.WriteLine("QUERY HANDLER PROCESSORS LOADINT TERMINATED:\n{0}", ex.Message);
            }
            ConsoleDraw.Primitives.DrawSpacedLine();
            Console.WriteLine();
            #endregion

            #region Start queries monitor threads
            for (int i = 0; i < threadsCount; i++)
            {
                // Instiniate server.
                Server serverBufer = new Server();
                longTermServerThreads[i] = serverBufer;

                // Set fields.
                serverBufer.pipeName = "dnfAUTH";

                // Starting server loop.
                serverBufer.StartServerThread(
                    "Queries monitor #" + i, serverBufer,
                    ThreadingServerLoop_PP_Input);

                // Change thread culture.
                serverBufer.thread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");

                // Skip line
                Console.WriteLine();
            }

            /// Draw line
            ConsoleDraw.Primitives.DrawLine();
            Console.WriteLine();
            #endregion

            #region Guest tokens broadcasting
            // Start broadcasting server that would share guest tokens.
            UniformServer.Standard.BroadcastingServer.StartBroadcastingViaPP(
                "guests",
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);
            #endregion

            // Show help.
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
            //AuthorityController.Session.InformateRelatedServers -= InformateRelatedServers;
            AuthorityController.API.LocalUsers.DirectoryLoadingFinished -= Users_DirectoryLoadingUnlocked;

            // Whait until close.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            #endregion
        }

        private static void Users_DirectoryLoadingUnlocked(string unlockedDirectory, int sucess, int failed)
        {
            // If users storage loaded.
            if (unlockedDirectory.Equals(Config.Active.UsersStorageDirectory))
            {
                // Mark users like unlocked.
                UsersLoaded = true;

                // TODO Check super admin existing.
            }
        }
    }
}
