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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniformQueries;
using UniformClient;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;
using AuthorityController.Data.Temporal;
using PipesProvider.Networking.Routing;
using AC_API = AuthorityController.API;

namespace ACTests.Tests
{
    [TestClass]
    public class Session
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Stop all started servers.
            PipesProvider.Server.ServerAPI.StopAllServers();
        }

        /// <summary>
        /// Try to get user by token.
        /// </summary>
        [TestMethod]
        public void UserByToken()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Get token info.
                if (!AuthorityController.Session.Current.TryGetTokenInfo(
                    Helpers.Users.user_User.tokens[0],
                    out TokenInfo info))
                {
                    Assert.Fail("User by token " + Helpers.Users.user_User.tokens[0] + " not found");
                    return;
                }

                // Get user by registred id.
                if(!AuthorityController.API.Users.TryToFindUser(
                    info.userId, 
                    out User user))
                {
                    Assert.Fail("User with id " + info.userId + " not exist");
                    return;
                }

                // All passed as expected.
                Assert.IsTrue(true);
            }
        }
       
        /// <summary>
        /// Try to logon by multiply devices.
        /// </summary>
        [TestMethod]
        public void MultiLogon()
        {
            // How many time will procceded logon.
            int logonsCount = 30;

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer(logonsCount);

                int startedServers = PipesProvider.Server.ServerAPI.SeversThreadsCount;
                if (startedServers < logonsCount)
                {
                    Assert.Fail(
                        "Started servers' threads less than requested. " +
                        PipesProvider.Server.ServerAPI.SeversThreadsCount +
                        "/" + logonsCount);
                    return;
                }

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                string operationError = null;

                // Array that would contain recived tokens.
                Hashtable tokens = new Hashtable();

                // Request every token.
                for (int i = 0; i < logonsCount; i++)
                {
                    int indexBufer = i;

                    #region Create logon query
                    // Create the query that would simulate logon.
                    QueryPart[] logonQuery = new QueryPart[]
                    {
                        // TODO INVALID TOKEN
                        new QueryPart("token", AuthorityController.API.Tokens.UnusedToken),
                        new QueryPart("guid", Guid.NewGuid().ToString()),

                        new QueryPart("user", null),
                        new QueryPart("logon", null),

                        new QueryPart("login", "sadmin"),
                        new QueryPart("password", "password"),
                        new QueryPart("os", Environment.OSVersion.VersionString),
                        new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                        new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                    };
                    #endregion

                    int callbacks = 0;
                    // Start reciving clent line.
                    BaseClient.EnqueueDuplexQueryViaPP("localhost", Helpers.Networking.DefaultQueriesPipeName,
                        QueryPart.QueryPartsArrayToString(logonQuery),
                        (PipesProvider.Client.TransmissionLine line, object answer) =>
                        {
                            callbacks++;
                            // Validate logon data.
                            bool logonResult = LogonValidator(answer, out string message);

                            // Break test with error.
                            if (!logonResult)
                            {
                                operationError = message;
                                operationCompete = true;
                                return;
                            }

                            // Chek token conflict.
                            if (tokens.Contains(message))
                            {
                                operationError = "The same token already provided.";
                                operationCompete = true;
                            }
                            else
                            {
                                // Save token.
                                tokens.Add(message, message);
                            }
                        });

                    Thread.Sleep(5);
                }

                // Wait until operation would complete.
                while (
                    !operationCompete &&
                    tokens.Count < logonsCount
                    )
                {
                    Thread.Sleep(5);
                }

                // Get result.
                Assert.IsTrue(string.IsNullOrEmpty(operationError), operationError);
            }
        }

        /// <summary>
        /// Validate recived logon information.
        /// </summary>
        /// <param name="serverAnswer">Message recived from server.</param>
        /// <param name="message">Error message if is invalid. Toke if is valid.</param>
        /// <returns></returns>
        public static bool LogonValidator(object serverAnswer, out string message)
        {
            // Trying to convert answer to string
            if (serverAnswer is string answerS)
            {
                // Is operation success?
                if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                {
                    // Log error.
                    message = "Recived error:\n" + answerS;
                    return false;
                }
                else
                {
                    // Trying to get toekn from answer.
                    if (UniformQueries.API.TryGetParamValue("token", out string value, answerS))
                    {
                        // Confirm logon.
                        message = value;
                        return true;
                    }
                    else
                    {
                        // Log error.
                        message = "Answer not contain token:\nFull answer:" + answerS;
                        return false;
                    }
                }
            }
            else
            {
                // Assert error.
                message = "Incorrect format of answer. Required format is string. Type:" + serverAnswer.GetType();
                return false;
            }
        }
    }
}
