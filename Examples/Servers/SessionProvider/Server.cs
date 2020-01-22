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
using UniformServer;

namespace SessionProvider
{
    /// <summary>
    /// Server tasks:
    /// - Provide session token to clients.
    /// - User data managment.
    /// - Control rights.
    /// </summary>
    public class Server : BaseServer
    {
        public static bool UsersLoaded
        {
            get;
            private set;
        }

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
            // Check direcroties
            UniformDataOperator.AssembliesManagement.AssembliesHandler.LoadAssemblies(
                AppDomain.CurrentDomain.BaseDirectory + "libs\\");

            // Looking for replaced types that could be used by handlers.
            UniformDataOperator.AssembliesManagement.Modifiers.TypeReplacer.RescanAssemblies();


            // React on uniform arguments.
            ServerAppConfigurator.ArgsReactor(args);
            // react on args specified to that server.
            CustomArgsReactor(args);
            #endregion

            #region Initialize authority controller
            // Subscribe to events.
            //AuthorityController.Session.InformateRelatedServers += InformateRelatedServers;

            // Load users.
            AuthorityController.API.LocalUsers.DirectoryLoadingFinished += Users_DirectoryLoadingUnlocked;
            AuthorityController.API.LocalUsers.LoadProfilesAsync(Config.Active.UsersStorageDirectory);
            #endregion

            #region Loaded query handler processors.
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
                Console.WriteLine("QUERY HANDLER PROCESSORS LOADINT TERMINATED:\n{0}", ex.Message);
            }
            ConsoleDraw.Primitives.DrawSpacedLine();
            Console.WriteLine();
            #endregion

            #region Start queries monitor threads
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                // Instiniate server.
                Server serverBufer = new Server
                {
                    // Set fields.
                    pipeName = "dnfAUTH"
                };

                // Starting server loop.
                serverBufer.StartServerThread(
                    "Queries monitor #" + i, serverBufer,
                    ThreadingServerLoop_PP_Input);

                // Change thread culture.
                serverBufer.ServerThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");

                // Skip line
                Console.WriteLine();
            }

            // Draw line
            ConsoleDraw.Primitives.DrawLine();
            Console.WriteLine();
            #endregion

            #region Guest tokens broadcasting
            // Start broadcasting server that would share guest tokens.
            UniformServer.Standard.BroadcastServer.StartBroadcastingViaPP(
                "guests",
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);
            #endregion

            // Show help.
            CustomComands("help");

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
                    if (!CustomComands(command))
                    {
                        UniformServer.Commands.BaseCommands(command);
                    }
                }
                Thread.Sleep(UniformServer.ServerAppConfigurator.PreferedThreadsSleepTime);
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

        /// <summary>
        /// Processing arguments suitable to that server.
        /// </summary>
        /// <param name="args"></param>
        public static void CustomArgsReactor(params string[] args)
        {
            foreach(string arg in args)
            {
                // If Unifor sql operator is specified.
                if(arg.StartsWith("sql="))
                {
                    // Receiving code.
                    string operatorCode = null;
                    try { operatorCode = arg.Substring(4); }
                    catch
                    {
                        Console.WriteLine("Invalid argument. Allowed format sql:[DataOpratorCode].\nImplemented udo codes:\n\tmysql.");
                        continue;
                    }

                    bool success = true;
                    // Inint SqlOperatorHandler
                    switch (operatorCode)
                    {
                        case "mysql":
                            UniformDataOperator.Sql.SqlOperatorHandler.Active =
                                UniformDataOperator.Sql.MySql.MySqlDataOperator.Active;
                            break;

                        default:
                            success = false;
                            Console.WriteLine("Invalid argument. Undifined SQL data operator '" + operatorCode + "'.");
                            break;
                    }

                    if (success)
                    {
                        // Configuratie connection to SQL server.
                        InitSqlOperator();
                    }
                    continue;
                }
            }
        }

        /// <summary>
        /// Requiesting all required dat afor SQL operator work.
        /// </summary>
        public static void InitSqlOperator()
        {
            // Drop if not required.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active == null)
                return;

            // Init like MySql operator.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active is UniformDataOperator.Sql.MySql.MySqlDataOperator)
            {
                string error = null;
                do
                {
                    // Log error.
                    if(!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("\nConnection failed. Details:" + error);
                        ConsoleDraw.Primitives.DrawSpacedLine();
                    }

                    Console.Write("Enter MySql user's login: ");
                    UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.UserId = Console.ReadLine();

                    Console.Write("Enter MySql user's password: ");
                    UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.Password = Console.ReadLine();

                    // Call initialization with that data.
                    UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.Initialize();
                }
                while (!UniformDataOperator.Sql.SqlOperatorHandler.Active.OpenConnection(out error)); // Try to establish connection.
                UniformDataOperator.Sql.SqlOperatorHandler.Active.CloseConnection(); // Close oppened connection.

                // Clearing console to prevent storing of secrete data.
                Console.Clear();

                // Share info.
                Console.WriteLine("SQL Server connected. Console had been cleared to prevent storing of the secret data.");
                CustomComands("help");
                Console.WriteLine();
            }

            // Validating all shemas and tables.
            UniformDataOperator.Sql.SqlOperatorHandler.RescanDatabaseStructure();
        }

        /// <summary>
        /// Perform command suitable for that server.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Is command executed.</returns>
        public static bool CustomComands(string command)
        {
            if(command.Equals("help"))
            {
                UniformServer.Commands.BaseCommands("help");

                Console.WriteLine("ADDITIVE COMMANDS:\n" +
                    "sql=[OperatorCode] - Connect to specified SQL data server. " +
                    "Implemeted codes: 'mysql'");

                ConsoleDraw.Primitives.DrawLine();

                return true;
            }

            // If that is configurator of SQL operator.
            if(command.StartsWith("sql="))
            {
                // Send to args processing.
                CustomArgsReactor(command);
                return true;
            }

            return false;
        }
    }
}
