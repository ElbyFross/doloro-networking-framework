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
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PipesProvider.Networking.Routing;
using PipesProvider.Security;
using PipesProvider.Client;
using UniformQueries;

namespace ExampleClient
{
    /// <summary>
    /// Provide example of client-server transmission.
    /// Demostrate ways to use API.
    /// Provide tools for simple tests of local server.
    /// </summary>
    class Client : UniformClient.BaseClient
    {
        /// <summary>
        /// Data about target server loaded from routing table.
        /// </summary>
        public static Instruction routingInstruction;

        /// <summary>
        /// Server that will be used as target for this client.
        /// </summary>
        public static string SERVER_NAME = null;//"192.168.1.74"; // Dot equal to local.

        /// <summary>
        /// Pipe that will be used to queries of this client.
        /// </summary>
        public static string SERVER_PIPE_NAME = null; // Pipe openned at server that will recive out queries.

        /// <summary>
        /// Is guest token required.
        /// </summary>
        public static bool guestTokenRequired = true;
        
        static void Main(string[] args)
        {
            #region Encryption test.
            //string enc = PipesProvider.Security.Crypto.EncryptString("Encryption Test", PipesProvider.Security.Crypto.PublicKey);
            //string dec = PipesProvider.Security.Crypto.DecryptString(enc);

            //Console.WriteLine("Result: {0}",dec);
            //Console.ReadLine();
            #endregion

            // This part has no any effect on application in future. Valid routing table would be loaded from "resources\routing\ROUTING.xml".
            //
            // Client could has a numbers of connections to different servers.
            // Connection to each server can have differend params.
            //
            // Cases:
            // Some servers can require system login to pas LSA of Windows NT.
            // Some could require in application authorization.
            #region Routing table serialization example
            // Create table with differend delivered instruction's types.
            routingTable.intructions = new System.Collections.Generic.List<Instruction>(new Instruction[]
                {
                new Instruction()
                {
                    routingIP = "localhost",
                    pipeName = "THB_QUERY_SERVER",
                    logonConfig = new LogonConfig(null, null, "WORKGROUP"),
                    title = "Query server",
                    commentary = "Server that reciving all queries from clients and then forwarding them to target servers."
                },
                new PartialAuthorizedInstruction()
                {
                    guestChanel = "guest",
                    routingIP = "localhost",
                    title = "Guest auth server",
                    commentary = "Authority Server that maintaining broadcast of guest tokens."
                }});
            RoutingTable.SaveRoutingTable(routingTable, "resources/sample", "ROUTING"); // Save table as recommended sample for this client.
            routingTable = new RoutingTable(); // Drop from memory to allow clear loading of customer's routing tables.
            #endregion

            #region Init
            // React on uniform arguments.
            UniformClient.ClientAppConfigurator.ArgsReactor(args);

            // Check direcroties
            UniformDataOperator.AssembliesManagement.AssembliesHandler.LoadAssemblies(
                AppDomain.CurrentDomain.BaseDirectory + "libs\\");

            // Looking for replaced types that could be used by handlers.
            UniformDataOperator.AssembliesManagement.Modifiers.TypeReplacer.RescanAssemblies();

            // Loading roting tables to detect servers.
            LoadRoutingTables(AppDomain.CurrentDomain.BaseDirectory + "plugins\\");
            Thread.Sleep(50);

            Console.WriteLine("Preparetion finished. Client strated.");
            #endregion

            // This client has only one server so we just looks on first included instruction in routing table,
            // to detect the target params.
            routingInstruction = routingTable.intructions[0];

            // Set loaded config to firlds.
            // Provided only for code simplifying and showing example of hard coded data using.
            // In normal way I recommend to use direct call of ServerMeta's fields and properties.
            SERVER_NAME = routingInstruction.routingIP;
            SERVER_PIPE_NAME = routingInstruction.pipeName;


            // Try to make human clear naming of server. In case of local network we will get the machine name.
            // This is optional and not required for stable work, just little helper for admins.
            PipesProvider.Networking.Info.TryGetHostName(SERVER_NAME, ref SERVER_NAME);
            Console.WriteLine("Work with a server by the route: " + SERVER_NAME + "." + SERVER_PIPE_NAME);
                  

            // Check server exist. When connection will be established will be called shared delegate.
            // Port 445 required for named pipes work.
            Console.WriteLine("Ping the host server via the 445 port...");
            PipesProvider.Networking.Info.PingHost(
                SERVER_NAME, 445,
                delegate (string uri, int port)
                {
                    // Log about success ping operation.
                    Console.WriteLine("PING COMPLITED | HOST AVAILABLE | {0}:{1}\n", uri, port);
                    
                    #region Recive guest token from server
                    // Trying to get instruction in partial authorized format.
                    if (routingInstruction is PartialAuthorizedInstruction partialAuthorizedInstruction)
                    {
                        // Trying to recive guest token from server.
                        _ = partialAuthorizedInstruction.TryToGetGuestTokenAsync(
                            AuthorityController.Session.Current.TerminationTokenSource.Token); // Using Sesstion termination token as uniform 
                                                                                               //to provide possibility to stop all async operation before application exit.
                    }
                    else
                    {
                        Console.WriteLine(
                            "ERROR: Invalid cast. For this example routing " +
                            "instruction by 0 index must by delivered from" +
                            " PartialAuthorizedInstruction. Application terminated.");
                        Thread.Sleep(2000);
                        return;
                    }

                    // Wait until authorization.
                    Console.WriteLine("Waiting for a guest token from server's authority system...");
                    while (!partialAuthorizedInstruction.GuestTokenHandler.IsAutorized)
                    {
                        Thread.Sleep(50);
                    }
                    Console.WriteLine("Authorized. Token: " + partialAuthorizedInstruction.GuestToken);
                    #endregion

                    // Send few example queries to server.
                    TransmissionsBlock();
                });
            
            #region Main loop
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    Console.Write("Enter command: ");
                    string tmp = Console.ReadLine();

                    // Skip empty requests.
                    if (string.IsNullOrEmpty(tmp)) continue;

                    // Close application by command.
                    if (tmp == "close") break;
                    else
                    {
                        // If included counter then spawn a echo in loop.
                        if (Int32.TryParse(tmp, out int repeaterRequest))
                        {
                            for (int i = 1; i < repeaterRequest + 1; i++)
                            {
                                SendOneWayQuery("ECHO" + i + "/" + repeaterRequest);
                            }
                        }
                        // Share custom query.
                        else
                        {
                            // Send as duplex.
                            if (tmp.StartsWith("DPX:"))
                            {
                                EnqueueDuplexQueryViaPP(SERVER_NAME, SERVER_PIPE_NAME,
                                    new Query(tmp.Substring(4)), ServerAnswerHandler_RSAPublicKey).
                                    TryLogonAs(routingInstruction.logonConfig);
                            }
                            // Send as one way
                            else
                            {
                                SendOneWayQuery(tmp);
                            }
                        }
                    }
                }
            }
            #endregion

            // Close all active lines. Without this operation thread will be hanged.
            ClientAPI.CloseAllTransmissionLines();

            // Whait until close.
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Method that will send few sample to remote server.
        /// </summary>
        static void TransmissionsBlock()
        {
            // If requested line encryption.
            if (routingInstruction.encryption)
            {
                Console.WriteLine("Wait for public key....");
                while (!routingInstruction.IsValid)
                {
                    Thread.Sleep(50);
                }
            }

            // Pause between queries to more clear console logging.
            // In normal state this not required. Queries included DNS system auto controling stability.
            // Your queries in safe and not require manual control.
            int pauseBetweenQueries = 2000;

            #region First query
            ConsoleDraw.Primitives.DrawSpacedLine();
            Console.WriteLine("ONE WAY query.\nTransmisssion to {0}/{1}", SERVER_NAME, SERVER_PIPE_NAME);

            // Short way to send one way query.
            OpenOutTransmissionLineViaPP(SERVER_NAME, SERVER_PIPE_NAME). // Opern transmission line via starndard DNS handler.
                EnqueueQuery(new Query(     // Adding query to line's queue.
                    new QueryPart("token", ((PartialAuthorizedInstruction)routingInstruction).GuestToken), // Using recent recived guest token for authorization.
                    new QueryPart("guid", "echo"),
                    new QueryPart("q", "ECHO"))).
                SetInstructionAsKey(ref routingInstruction).        // Connect instruction to provide auto-encryption via RSA.
                TryLogonAs(routingInstruction.logonConfig);         // Request remote logon. By default LogonConfig equal Anonymous (Guest) user.
            #endregion

            #region Second query
            Thread.Sleep(pauseBetweenQueries);
            ConsoleDraw.Primitives.DrawSpacedLine();
            Console.WriteLine("ONE WAY (SHORT FORMAT) query.\nTransmisssion to {0}/{1}", SERVER_NAME, SERVER_PIPE_NAME);

            // Send sample one way query to server with every step description.
            SendOneWayQuery(string.Format("token={1}{0}guid=datesRange{0}q=GET{0}sq=DAYSRANGE", UniformQueries.API.SPLITTING_SYMBOL,
                ((PartialAuthorizedInstruction)routingInstruction).GuestToken)); // Using recent recived guest token.
            #endregion

            #region Third query
            Thread.Sleep(pauseBetweenQueries);
            ConsoleDraw.Primitives.DrawSpacedLine();
            Console.WriteLine("DUPLEX query.\nTransmisssion to {0}/{1}", SERVER_NAME, SERVER_PIPE_NAME);

            // Get public key for RSA encoding from target server.
            RequestPublicRSAKey();
            #endregion
        }

        #region Queries
        static void SendOneWayQuery(string query)
        {
            #region Authorizing on remote machine
            // Get rights to access remote machine.
            //
            // If you use anonymous conection than you need to apply server's LSA (LocalSecurityAuthority) rules:
            // - permit Guest connection over network.
            // - activate Guest user.
            // Without this conection will terminated by server.
            //
            // Relative to setting of pipes also could be required:
            // - anonymous access to named pipes
            //
            // ATTENTION: Message will not be encrypted before post. 
            // User SetRoutingInstruction (whrer instruction has RSAEncryption fields as true) instead TryLogon.
            bool logonResult = General.TryToLogonAtRemoteDevice(
                routingInstruction.logonConfig,
                out SafeAccessTokenHandle safeTokenHandle);
            if (!logonResult)
            {
                Console.WriteLine("Logon failed. Connection not possible.\nPress any key...");
                Console.ReadKey();
                return;
            }
            #endregion

            // Create transmission line.
            TransmissionLine lineProcessor = OpenOutTransmissionLineViaPP(SERVER_NAME, SERVER_PIPE_NAME);
            // Set impersonate token.
            lineProcessor.accessToken = safeTokenHandle;

            // Add sample query to queue. You can use this way if you not need answer from server.
            lineProcessor.EnqueueQuery(new Query(query));
        }

        static void RequestPublicRSAKey()
        {
            // Create query that request public RSA key of the server. 
            //This will allow to us encrypt queries and shared data befor transmission in future.
            //
            // Format: param=value&param=value&...
            // "guid", "token" and "q" (query) required.
            //
            // Param "pk" (public key (RSA)) will provide possibility to encrypt of answer on the server side.
            //
            // Using a UniformQueries.API.SPLITTING_SYMBOL to get a valid splitter between your query parts.
            Query GetPKQuery = new Query(
                new QueryPart("guid", "WelomeGUID"),
                new QueryPart("token", "InvalidToken"),
                new QueryPart("get"),
                new QueryPart("publickey"));

            // Opens duplex chanel. At first the line will send the query to a server
            // and after that will listen to its answer.
            // When answer will receive it will redirected to the callback.
            EnqueueDuplexQueryViaPP(SERVER_NAME, SERVER_PIPE_NAME, 
                GetPKQuery, ServerAnswerHandler_RSAPublicKey).
                TryLogonAs(routingInstruction.logonConfig); // Share logon cofig to allow connectio for not public servers.

            // Let the time to transmission line to qompleet the query.
            Thread.Sleep(150);
        }
        #endregion


        #region Server's answer callbacks
        // Create delegate that will recive and procced the server's answer.
        static void ServerAnswerHandler_RSAPublicKey(TransmissionLine tl, object message)
        {
            string messageS = message as string;
            Console.WriteLine("RSA Public Key recived:\n" + (messageS ?? "Message is null"));
        }
        #endregion
    }
}
