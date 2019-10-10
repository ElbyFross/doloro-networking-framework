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
using System.Threading;
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;

namespace BaseQueries
{
    /// <summary>
    /// Registrate token with guest rights in the system and return to client.
    /// </summary>
    public class GET_GUEST_TOKEN : IQueryHandler
    {
        /// <summary>
        /// Handler that would be userd to generating and authorizing of guest tokens.
        /// Return generated token in string format.
        /// </summary>
        public static GuestTokenHandler guestTokenHandler;

        /// <summary>
        /// Delegate that allows to return guest token in string format.
        /// </summary>
        /// <returns></returns>
        public delegate string GuestTokenHandler();

        /// <summary>
        /// Return the description relative to the lenguage code or default if not found.
        /// </summary>
        /// <param name="cultureKey">Key of target culture.</param>
        /// <returns>Description for relative culture.</returns>
        public string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "GET GUEST TOKEN\n" +
                            "\tDESCRIPTION: Provide guest key for passing of base authority level\n" +
                            "\tQUERY FORMAT: GET" + UniformQueries.API.SPLITTING_SYMBOL + "GUEST" +
                            UniformQueries.API.SPLITTING_SYMBOL + "TOKEN\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public void Execute(object sender, Query query)
        {
            if (guestTokenHandler != null)
            {
                // Send token to client.
                UniformServer.BaseServer.SendAnswerViaPP(guestTokenHandler.Invoke(), query);
            }
            else
            {
                UniformServer.BaseServer.SendAnswerViaPP("Error: Server unable to generate guest token.", query);
            }
        }

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="query">Recived query.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(Query query)
        {
            if (!query.QueryParamExist("get"))
                return false;

            if (!query.QueryParamExist("guest"))
                return false;

            if (!query.QueryParamExist("token"))
                return false;

            return true;
        }

        /// <summary>
        /// Handler that provide standartized way to recive guest token.
        /// </summary>
        public class GuestTokenProcessor : UniformQueries.Executable.Security.AuthQueryProcessor
        {
            /// <summary>
            /// Trying to recive guest token in async task.
            /// </summary>
            /// <param name="serverIP">Target server's IP.</param>
            /// <param name="pipeName">Name of the pipe that broadcasting guest tokens.</param>
            /// <param name="cancellationToken">Token that would terminate async operation.</param>
            public async void TryToReciveTokenAsync(
                  string serverIP,
                  string pipeName,
                  CancellationToken cancellationToken)
            {
                #region Validate
                // Drop if process already started to avoid conflicts.
                if (IsInProgress)
                {
                    Console.WriteLine("Authorization process already started.");
                    return;
                }
                #endregion

                #region Set markers
                // Drop previous autorization.
                IsAutorized = false;
                IsInProgress = true;
                IsTerminated = false;
                #endregion

                #region Wait connection possibilities.
                if (!PipesProvider.NativeMethods.DoesNamedPipeExist(serverIP, pipeName))
                {
                    await Task.Run(() =>
                    {
                        // Check server pipe existing.
                        while (!PipesProvider.NativeMethods.DoesNamedPipeExist(serverIP, pipeName))
                        {
                            // Terminate task.
                            if (IsTerminated)
                            {
                                // Disable in progress marker.
                                IsInProgress = false;

                                return;
                            }

                            // Wait if not found.
                            Thread.Sleep(500);
                        }
                    },
                    cancellationToken);
                }
                #endregion

                //Recive message.
                UniformClient.BaseClient.ReceiveAnonymousBroadcastMessage(
                   serverIP, pipeName,
                   ServerAnswerHandler);
            }
        }
    }
}
