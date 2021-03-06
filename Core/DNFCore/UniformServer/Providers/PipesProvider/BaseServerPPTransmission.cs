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
    /// Part of class that provide methods for establishing transmisssion via PipesProvider.
    /// </summary>
    public partial class BaseServer
    {
        /// <summary>
        /// Open server line using PipesProvider that will send answer backward to cliend by dirrect line.
        /// Line will established relative to the data shared by client query.
        /// 
        /// Using this method you frovide uniform revers connection and not need to create 
        /// a transmission line by yourself.
        /// 
        /// Recommended to use this methos by default dor duplex connection between sever and clients.
        /// </summary>
        /// <param name="answer">Message that will sent by server to target client.</param>
        /// <param name="entryQuery">Query that was recived from client. 
        /// Method will detect core part and establish backward connection.</param>
        /// <returns></returns>
        public static bool SendAnswerViaPP(string answer, UniformQueries.Query entryQuery)
        {
            return SendAnswerViaPP(new UniformQueries.Query(answer), entryQuery);
        }

        /// <summary>
        /// Open server line using PipesProvider that will send answer backward to cliend by dirrect line.
        /// Line will established relative to the data shared by client query.
        /// 
        /// Using this method you frovide uniform revers connection and not need to create 
        /// a transmission line by yourself.
        /// 
        /// Recommended to use this methos by default dor duplex connection between sever and clients.
        /// </summary>
        /// <param name="answer">Qury that will sent by server to target client.</param>
        /// <param name="entryQuery">Query that was recived from client. 
        /// Method will detect core part and establish backward connection.</param>
        /// <returns></returns>
        public static bool SendAnswerViaPP(UniformQueries.Query answer, UniformQueries.Query entryQuery)
        {
            // Instiniate primitive server to provide loop.
            BaseServer server = new BaseServer();

            // Try to compute bacward domaint to contact with client.
            if (!UniformQueries.QueryPart.TryGetBackwardDomain(entryQuery, out string domain))
            {
                Console.WriteLine("ERROR (BSSA0): Unable to buid backward domain. QUERY: " + entryQuery.ToString());
                return false;
            }

            // Set fields.
            server.pipeName = domain;

            // Subscribe or waiting delegate on server loop event.
            ServerAPI.TransmissionToProcessing += InitationCallback;


            // Starting server loop.
            server.StartServerThread(
                "SERVER ANSWER " + domain, server,
                ThreadingServerLoop_PP_Output);

            // Skip line
            Console.WriteLine();
            return true;

            // Create delegate that will set our answer message to processing
            // when transmission line would established.
            void InitationCallback(BaseServerTransmissionController tc)
            {
                if (tc is ServerToClientTransmissionController transmissionController)
                {
                    // Target callback.
                    if (transmissionController.PipeName == server.pipeName)
                    {
                        // Unsubscribe.
                        ServerAPI.TransmissionToProcessing -= InitationCallback;

                        bool encryptingComplete = false;

                        // Encrypting data in async operation.
                        var encryptionAgent = new System.Threading.Tasks.Task(async delegate ()
                        {
                            // Try to encrypt answer if required.
                            await PipesProvider.Security.Encryption.EnctyptionOperatorsHandler.
                            TryToEncryptByReceivedQueryAsync(entryQuery, answer, CancellationToken.None);

                            encryptingComplete = true;
                        });

                        try
                        {
                            encryptionAgent.Start();
                        }
                        catch { }

                        // Wait for encryption 
                        while(!encryptingComplete)
                        {
                            Thread.Sleep(5);
                        }

                        // Set answer query as target for processing,
                        transmissionController.ProcessingQuery = answer;

                        // Log.
                        Console.WriteLine(@"{0}: Processing query changed on: " + @answer.ToString(), @transmissionController.PipeName);
                    }
                }
                else // Incorrect type.
                {
                    // Close transmisssion.
                    tc.SetStoped();

                    // Log.
                    Console.WriteLine("{0}: ERROR Incorrect transmisssion controller. Required \"ServerAnswerTransmissionController\"", tc.PipeName);
                }
            }
        }
    }
}
