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
        public delegate string GuestTokenHandler();

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

        public void Execute(QueryPart[] queryParts)
        {
            if (guestTokenHandler != null)
            {
                // Send token to client.
                UniformServer.BaseServer.SendAnswerViaPP(guestTokenHandler.Invoke(), queryParts);
            }
            else
            {
                UniformServer.BaseServer.SendAnswerViaPP("Error: Server unable to generate guest token.", queryParts);
            }
        }

        public bool IsTarget(QueryPart[] queryParts)
        {
            if (!UniformQueries.API.QueryParamExist("get", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("guest", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("token", queryParts))
                return false;

            return true;
        }

        /// <summary>
        /// Handler that provide standartized way to recive guest token.
        /// </summary>
        public class GuestTokenProcessor : UniformQueries.Executable.Security.AuthQueryProcessor
        {
            public async void TryToReciveTokenAsync(
                  string serverIP,
                  string pipeName,
                  CancellationToken cancellationToken)
            {
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
                UniformClient.Standard.SimpleClient.ReceiveAnonymousBroadcastMessage(
                   serverIP, pipeName,
                   ServerAnswerHandler);
            }
        }
    }
}
