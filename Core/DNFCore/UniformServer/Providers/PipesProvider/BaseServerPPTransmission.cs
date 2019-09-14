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
    /// Part of class that provide methods for establishing transmisssion via PipesProvider.
    /// </summary>
    public abstract partial class BaseServer
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
        /// <param name="entryQueryParts">Parts of query that was recived from client. 
        /// Method will detect core part and establish backward connection.</param>
        /// <returns></returns>
        public static bool SendAnswerViaPP(string answer, UniformQueries.QueryPart[] entryQueryParts)
        {
            // Instiniate primitive server to provide loop.
            BaseServer server = new Standard.SimpleServer();

            // Try to compute bacward domaint to contact with client.
            if (!UniformQueries.QueryPart.TryGetBackwardDomain(entryQueryParts, out string domain))
            {
                Console.WriteLine("ERROR (BSSA0): Unable to buid backward domain. QUERY: {0}",
                    UniformQueries.QueryPart.QueryPartsArrayToString(entryQueryParts));
                return false;
            }

            // Set fields.
            server.pipeName = domain;

            // Create delegate that will set our answer message to processing
            // when transmission line would established.
            void InitationCallback(BaseServerTransmissionController tc)
            {
                if (tc is ServerToClientTransmissionController transmissionController)
                {
                    // Target callback.
                    if (transmissionController.pipeName == server.pipeName)
                    {
                        // Unsubscribe.
                        ServerAPI.ServerTransmissionMeta_InProcessing -= InitationCallback;

                        #region Encryption
                        // Encrypt query if requested by "pk" query's param.
                        if (UniformQueries.API.TryGetParamValue(
                            "pk",
                            out UniformQueries.QueryPart publicKeyProp,
                            entryQueryParts))
                        {
                            // Try to get publick key from entry query.
                            if (PipesProvider.Security.Crypto.TryDeserializeRSAKey(publicKeyProp.propertyValue,
                                out System.Security.Cryptography.RSAParameters publicKey))
                            {
                                // Encrypt query.
                                answer = PipesProvider.Security.Crypto.EncryptString(answer, publicKey);
                            }
                        }
                        #endregion

                        // Set answer query as target for processing,
                        transmissionController.ProcessingQuery = answer;

                        // Log.
                        Console.WriteLine("{0}: Processing query changed on:\n{1}\n", transmissionController.pipeName, answer);
                    }
                }
                else // Incorrect type.
                {
                    // Close transmisssion.
                    tc.SetStoped();

                    // Log.
                    Console.WriteLine("{0}: ERROR Incorrect transmisssion controller. Required \"ServerAnswerTransmissionController\"", tc.pipeName);
                }
            }
            // Subscribe or waiting delegate on server loop event.
            ServerAPI.ServerTransmissionMeta_InProcessing += InitationCallback;


            // Starting server loop.
            server.StartServerThread(
                "SERVER ANSWER " + domain, server,
                ThreadingServerLoop_PP_Output);

            // Skip line
            Console.WriteLine();
            return true;
        }
    }
}
