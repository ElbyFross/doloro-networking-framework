﻿//Copyright 2019 Volodymyr Podshyvalov
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
using PipesProvider.Server.TransmissionControllers;

namespace QueriesServer
{
    /// <summary>
    /// Public servers that allow anonimous connection.
    /// Reciving clients' queries and redirect in to target infrastructure servers by the comutation table.
    /// Reciving answer from servers and redirect it to target clients.
    /// </summary>
    class Server : UniformServer.BaseServer
    {
        /// <summary>
        /// Main public pipe that will listen queries.
        /// </summary>
        public static string OPEN_CHANEL = "THB_QUERY_SERVER";

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


            // Request anonymous configuration for system.
            General.SetLocalSecurityAuthority(SecurityLevel.Anonymous);


            #region Load routing tables
            UniformClient.BaseClient.LoadRoutingTables(AppDomain.CurrentDomain.BaseDirectory + "plugins\\");
            #endregion

            #region Loaded query handler processors
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
                Console.WriteLine("QUERY HANDLER PROCESSORS LOADING TERMINATED:\n{0}", ex.Message);
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
                serverBufer.pipeName = OPEN_CHANEL;

                // Starting server loop.
                serverBufer.StartServerThread(
                    "Queries chanel #" + i, serverBufer,
                    ThreadingServerLoop_Relay);

                // Change thread culture.
                serverBufer.thread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");

                // Skip line
                Console.WriteLine();
            }
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

            // Aborting threads.
            foreach (Server st in longTermServerThreads)
            {
                st.thread.Abort();
                Console.WriteLine("THREAD ABORTED: {0}", st.thread.Name);
            }
            Console.WriteLine();

            // Whait until close.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            #endregion
        }

        /// <summary>
        ///  Start the server loop that will condtol relay query handler.
        /// </summary>
        protected static void ThreadingServerLoop_Relay(object server)
        {
            #region Init
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Name of pipe server that will established.
            // Access to this pipe by clients will be available by this name.
            string serverName = ((Server)server).thread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            PipesProvider.Server.TransmissionControllers.ClientToServerTransmissionController.ServerLoop(
                serverName,
                QueryHandler_Relay,
                ((Server)server).pipeName,
                ((Server)server).securityLevel);
            #endregion
        }

        /// <summary>
        /// Redirect recived query from current server to other.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="query"></param>
        public static void QueryHandler_Relay(BaseServerTransmissionController _, string query)
        {
            // Try to decrypt.
            query = PipesProvider.Security.Crypto.DecryptString(query);

            // Detect routing target.
            bool relayTargetFound = UniformClient.BaseClient.routingTable.TryGetRoutingInstruction(query, out Instruction instruction);

            // If instruction not found.
            if (!relayTargetFound)
            {
                // If reley target not found then server will mean that query requested to itself.
                PipesProvider.Handlers.Query.ProcessingAsync(_, query);

                //// Log
                //Console.WriteLine("RELAY TARGET NOT FOUND: {q}", query);

                //// DO BACKWARED ERROR INFORMATION.
                //SendAnswer("error=404", UniformQueries.API.DetectQueryParts(query));

                // Drop continue computing.
                return;
            }

            // If requested encryption.
            if (instruction.RSAEncryption)
            {
                // Check if instruction key is valid.
                // If key expired or invalid then will be requested new.
                if(!instruction.IsValid)
                {
                    // Request new key.
                    UniformClient.BaseClient.GetValidPublicKeyViaPP(instruction);

                    // Log.
                    Console.WriteLine("WAITING FOR PUBLIC RSA KEY FROM {0}/{1}", instruction.routingIP, instruction.pipeName);

                    // Wait until validation time.
                    // Operation will work in another threads, so we just need to take a time.
                    while(!instruction.IsValid)
                    {
                        Thread.Sleep(threadSleepTime);
                    }
                    
                    // Log.
                    Console.WriteLine("PUBLIC RSA KEY FROM {0}/{1} RECIVED", instruction.routingIP, instruction.pipeName);
                }

                // Encrypt query by public key of target server.
                query = PipesProvider.Security.Crypto.EncryptString(query, instruction.PublicKey);
            }

            // Open connection.
            TransmissionLine tl = UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                instruction.routingIP,
                instruction.pipeName,
                query,
                // Delegate that will called when relayed server send answer.
                // Redirect this answer to client.
                delegate (PipesProvider.Client.TransmissionLine answerTL, object answer)
                {
                    // Try to get answer in string format.
                    string answerAsString = answer as string;
                    if (!string.IsNullOrEmpty(answerAsString))
                    {
                        SendAnswerViaPP(answerAsString, UniformQueries.API.DetectQueryParts(query));
                        return;
                    }

                    // Try to get answer as byte array.
                    if (answer is byte[] answerAsByteArray)
                    {
                        // TODO Send answer as byte array.
                        throw new NotImplementedException();
                    }
                });
        }
    }
}
