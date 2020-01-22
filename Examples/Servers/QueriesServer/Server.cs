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
using PipesProvider.Server.TransmissionControllers;
using UniformServer;

namespace QueriesServer
{
    /// <summary>
    /// Public servers that allow anonimous connection.
    /// Reciving clients' queries and redirect in to target infrastructure servers by the comutation table.
    /// Reciving answer from servers and redirect it to target clients.
    /// </summary>
    public class Server : BaseServer
    {
        /// <summary>
        /// Main public pipe that will listen queries.
        /// </summary>
        public static string OPEN_CHANEL = "THB_QUERY_SERVER";

        static void Main(string[] args)
        {
            #region Detect processes conflicts
            // Check does this instance a new single app, or same app already runned.
            if (!ServerAppConfigurator.IsProccessisUnique())
            {
                // Log error.
                Console.WriteLine("\"THB Data Server\" already started. Application not allow multiple instances at single moment.");
                // Wait a time until exit.
                Thread.Sleep(2000);
                return;
            }
            #endregion
                        
            #region Set default data \ load DLLs \ appling arguments
            // Set default thread count. Can be changed via args or command.
            //threadsCount = Environment.ProcessorCount;
            //longTermServerThreads = new UniformServer.BaseServer[threadsCount];

            // React on uniform arguments.
            ServerAppConfigurator.ArgsReactor(args);
            
            // Check direcroties
            UniformDataOperator.AssembliesManagement.AssembliesHandler.LoadAssemblies(
                AppDomain.CurrentDomain.BaseDirectory + "libs\\");

            // Looking for replaced types that could be used by handlers.
            UniformDataOperator.AssembliesManagement.Modifiers.TypeReplacer.RescanAssemblies();
            #endregion


            // Request anonymous configuration for system.
            General.SetLocalSecurityAuthority(SecurityLevel.Anonymous);

            #region Load routing tables.
            // Try to load tables.
            UniformClient.BaseClient.LoadRoutingTables(AppDomain.CurrentDomain.BaseDirectory + "plugins\\");

            // Init new if not found.
            if (UniformClient.BaseClient.routingTable.intructions.Count == 0)
            {
                SetDefaultRoutingTable();
            }
            #endregion

            #region Loaded query handler processors
            // Draw line
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
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                // Configuring the routing instruction.
                RelayInstruction relayInstruction = new RelayInstruction()
                {
                    entryPipeName = OPEN_CHANEL
                };

                // Instiniating server.
                var serverBufer = UniformServer.Standard.RelayServer.EstablishDuplexRelayServer(relayInstruction);


                // Changing thread culture.
                serverBufer.ServerThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                // Skip line
                Console.WriteLine();
            }
            #endregion

            #region Start broadcast relaying chanels.
            foreach (Instruction instruction in UniformClient.BaseClient.routingTable.intructions)
            {
                // Looking for broadcasting relays.
                if(instruction is RelayInstruction relayInstruction && 
                    relayInstruction.behavior == RelayInstruction.RelayBehavior.Broadcasting)
                {
                    // Start relay server.
                    UniformServer.Standard.RelayServer.EstablishBroadcastingRelayServer(relayInstruction);
                }
            }
            #endregion

            // Show help.
            UniformServer.Commands.BaseCommands("help");

            #region Main loop
            // Main loop that will provide server services until application close.
            while (!UniformServer.ServerAppConfigurator.AppTerminated)
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
                Thread.Sleep(UniformServer.ServerAppConfigurator.PreferedThreadsSleepTime);
            }
            #endregion

            #region Finalize
            Console.WriteLine();

            // Stop started servers.
            ServerAPI.StopAllServers();

            // Whait until close.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            #endregion
        }

        /// <summary>
        /// Set default routing table's draft to resources\routing\ROUTING.xml file.
        /// </summary>
        public static void SetDefaultRoutingTable()
        {
            // Set public chanel.
            UniformClient.BaseClient.routingTable.intructions.Add(
                new RelayInstruction()
                {
                    title = "Public chanel",
                    behavior = RelayInstruction.RelayBehavior.Duplex,
                    entryPipeName = OPEN_CHANEL,
                    pipeName = "DATA_SERVER_PIPE",
                    encryption = true
                });

            // Set guset chanel.
            UniformClient.BaseClient.routingTable.intructions.Add(
                new RelayInstruction()
                {
                    title = "Guests chanel",
                    behavior = RelayInstruction.RelayBehavior.Broadcasting,
                    entryPipeName = "guests",
                    pipeName = "guests",
                    encryption = false
                });

            // Save table to resources as draft.
            RoutingTable.SaveRoutingTable(UniformClient.BaseClient.routingTable);
        }
    }
}
