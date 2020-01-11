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
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniformQueries;
using UniformServer;
using AuthorityController.Data;
using AuthorityController.Data.Personal;

namespace ACTests.Tests
{
    [TestClass]
    public class Queries
    {
        /// <summary>
        /// Provided guest token.
        /// </summary>
        public static string GUEST_TOKEN = null;

        #region Helpers
        /// <summary>
        /// Return ban information data ready for test.
        /// </summary>
        /// <param name="banInfoXML"></param>
        /// <param name="expiryTime"></param>
        public static BanInformation GetBanInfo(out DateTime expiryTime)
        {
            // Time when ban will expire.
            expiryTime = DateTime.Now.AddMilliseconds(500);

            // Create ban information.
            return new BanInformation()
            {
                active = true,
                blockedRights = new string[] { "logon" },
                commentary = "Test ban",
                duration = BanInformation.Duration.Temporary,
                expiryTime = expiryTime
            };
        }
        #endregion

        [TestCleanup]
        public void Cleanup()
        {
            // Stop all started servers.
            PipesProvider.Server.ServerAPI.StopAllServers();
        }

        #region Token rights
        /// <summary>
        /// Tryinging to get guest token using query.
        /// </summary>
        [TestMethod]
        public void GetGuestToken()
        {
            // Start broadcasting server that would share guest tokens.
            UniformServer.Standard.BroadcastServer.StartBroadcastingViaPP(
                Helpers.Networking.DefaultGuestPipeName,
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);

            // Marker.
            bool waitingAnswer = true;

            #region Server answer processing
            // Start listening client.
            UniformClient.Standard.SimpleClient.ReceiveAnonymousBroadcastMessage(
                "localhost", Helpers.Networking.DefaultGuestPipeName,
                (PipesProvider.Client.TransmissionLine line, Query answer) =>
                {
                    // Check token.
                    if (answer.TryGetParamValue("token", out QueryPart token))
                    {
                        // Set token as GUEST to share between other tests.
                        GUEST_TOKEN = token.PropertyValueString;

                        // Assert test result.
                        bool tokenProvided = !string.IsNullOrEmpty(token.PropertyValueString);
                        Assert.IsTrue(tokenProvided, "Token is null.\n" + answer.First.PropertyValueString);
                    }
                    else
                    {
                        // Inform that failed.
                        Assert.IsTrue(false, "Token not provided.\n" + answer.First.PropertyValueString);
                    }

                    // Check expire time.
                    if (!answer.QueryParamExist("expiryIn"))
                        Assert.IsTrue(false, "Expire time not provided.\n" + answer.First.PropertyValueString);

                    // Check rights providing.
                    if (!answer.QueryParamExist("rights"))
                        Assert.IsTrue(false, "Rights not shared.\n" + answer.First.PropertyValueString);

                    // Unlock finish blocker.
                    waitingAnswer = false;
                });
            #endregion

            // Wait server answer.
            while (waitingAnswer)
            {
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// Tryinging to update token rights without authority.
        /// </summary>
        [TestMethod]
        public void SetTokenRights_NoRights()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_User.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("set"),
                    new QueryPart("targetToken", Helpers.Users.user_Admin.tokens[0]),
                    new QueryPart("rights", "newRight")
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Unathorized operation passed. Server answer:" + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Tryinging to update token rights with walid permition.
        /// </summary>
        [TestMethod]
        public void SetTokenRights_HasRights()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_Admin.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("set"),
                    new QueryPart("targetToken", Helpers.Users.user_User.tokens[0]),
                    new QueryPart("rights", "newRight")
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (!answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Operation failed with error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }
        #endregion

        #region Ban
        /// <summary>
        /// Trying to ban user but has no rights to this.
        /// </summary>
        [TestMethod]
        public void UserBan_NoRights()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_User.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("ban", GetBanInfo(out DateTime expiryTime)),
                    new QueryPart("user", Helpers.Users.user_Guest.id.ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Permited unatorized operation. Answer:" + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to ban user that has a higher rank than requester.
        /// </summary>
        [TestMethod]
        public void UserBan_ModeratorToAdmin()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();
                
                // Create the query that would contain user data.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_Moderator.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("ban", GetBanInfo(out DateTime expiryTime)),
                    new QueryPart("user", Helpers.Users.user_Admin.id.ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Permited unatorized operation. Answer:" + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to ban user with enough rights to that.
        /// Confirm ban.
        /// Confirm ban expiring.
        /// </summary>
        [TestMethod]
        public void UserBan_FullCycle()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();
                
                #region Ban apply
                // Create the query that would contain user data.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_Admin.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("ban", GetBanInfo(out DateTime expiryTime)),
                    new QueryPart("user", Helpers.Users.user_User.id.ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (!answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Operation failed with error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });
                #endregion

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                #region Ban validation
                // Find banned user.
                if (!AuthorityController.API.LocalUsers.TryToFindUser(Helpers.Users.user_User.id, out User bannedUser))
                {
                    Assert.Fail("Banned user lossed");
                    return;
                }

                // Increase ban time if expired before test passing.
                if (DateTime.Compare(expiryTime, DateTime.Now) < 0)
                {
                    BanInformation bib = bannedUser.bans[0];
                    bib.expiryTime = expiryTime = DateTime.Now.AddSeconds(1);
                    bannedUser.bans[0] = bib;
                }

                // Check that still banned.
                if (!AuthorityController.Data.Personal.BanInformation.IsBanned(bannedUser, "logon"))
                {
                    Assert.Fail("User was not banned.");
                    return;
                }

                // Wait until epiry time.
                while (DateTime.Compare(expiryTime, DateTime.Now) > 0)
                {
                    Thread.Sleep(5);
                }

                // Check that not banned.
                if (AuthorityController.Data.Personal.BanInformation.IsBanned(bannedUser, "logon"))
                {
                    Assert.Fail("User still banned after ban's expiring.");
                    return;
                }
                #endregion

                Assert.IsTrue(operationResult, operationError);
            }
        }
        #endregion

        #region Logon
        /// <summary>
        /// Trying to logon as existed user with corerct logon data.
        /// </summary>
        [TestMethod]
        public void Logon_ValidData()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user"),
                    new QueryPart("logon"),

                    new QueryPart("login", "sadmin"),
                    new QueryPart("password", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()));

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Starting a client line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // A handler that will receive an answer from the server.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log about an error.
                            Assert.Fail("Recived error:\n" + answer.First.PropertyValueString);
                            operationCompete = true;
                        }
                        else
                        {
                            // Trying to get token from answer.
                            if (answer.TryGetParamValue("token", out QueryPart token) && 
                            !string.IsNullOrEmpty(token.PropertyValueString))
                            {
                                // Confirm logon.
                                Assert.IsTrue(true);
                                operationCompete = true;
                            }
                            else
                            {
                                // Log about an error.
                                Assert.Fail("Answer not contain token:\nFull answer:" + answer.First.PropertyValueString);
                                operationCompete = true;
                            }
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// Trying to logon as existed user that not registred.
        /// </summary>
        [TestMethod]
        public void Logon_UserNotExist()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user"),
                    new QueryPart("logon"),

                    new QueryPart("login", "notExistedUser"),
                    new QueryPart("password", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            Assert.IsTrue(true);
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            Assert.Fail("Unexisted user found on server.\nAnswer:" + answer.First.PropertyValueString);
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// Trying to logon with incorrrect password.
        /// </summary>
        [TestMethod]
        public void Logon_InvalidData()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user"),
                    new QueryPart("logon"),

                    new QueryPart("login", "user"),
                    new QueryPart("password", "invalidPassword"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,
                    query,
                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            Assert.IsTrue(true);
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            Assert.Fail("Unexisted user found on server.\nAnswer:" + answer.First.PropertyValueString);
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }
            }
        }
        #endregion

        #region Logoff
        /// <summary>
        /// Trying to logoff user by invalid token.
        /// </summary>
        [TestMethod]
        public void Logoff_InvalidToken()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Generate new token.
                string newToken = UniformQueries.Tokens.UnusedToken;

                // Logoff unregistred token.
                bool result = AuthorityController.Queries.USER_LOGOFF.LogoffToken(newToken);

                // Assert that token was rejected by a system.
                // If token was processed that this mean that system failed.
                Assert.IsTrue(!result, "Token detected, that can't be true.");
            }
        }

        /// <summary>
        /// Trying to logoff user by valid token.
        /// </summary>
        [TestMethod]
        public void Logoff_ValidToken()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Get token of registred user.
                string userToken = Helpers.Users.user_User.tokens[0];

                // Logoff unregistred token.
                bool result = AuthorityController.Queries.USER_LOGOFF.LogoffToken(userToken);

                // Assert that token was rejected by a system.
                // If token was processed that this mean that system failed.
                Assert.IsTrue(result, "Token not detected.");
            }
        }
        #endregion

        #region New user
        /// <summary>
        /// Trying to create user with valid data.
        /// </summary>
        [TestMethod]
        public void User_ValidData()
        {
            #region Getting guest token to logon on server
            if (string.IsNullOrEmpty(GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = Helpers.Users.NewUserQuery("login123@", "Password123!", "Mark", "Sanders");

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (!answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "User creation returned error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to logoff invalid token.
        /// </summary>
        [TestMethod]
        public void User_InvalidPassword()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = Helpers.Users.NewUserQuery("login123@", "aa!", "Mark", "Sanders");

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "User creation returned error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to create new user with invalid login.
        /// </summary>
        [TestMethod]
        public void User_InvalidLogin()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = Helpers.Users.NewUserQuery("a", "validPass2@", "Mark", "Sanders");

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "User creation returned error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to create new user that already exist.
        /// </summary>
        [TestMethod]
        public void User_UserExist()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Start new server for thsi test to avoid conflicts.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = Helpers.Users.NewUserQuery("admin", "validPass2@", "Mark", "Sanders");

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "User creation returned error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to create user with invalid personal data.
        /// </summary>
        [TestMethod]
        public void User_InvalidPersonal()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                Helpers.Networking.StartPublicServer();

                // Create the query that would contain user data.
                Query query = Helpers.Users.NewUserQuery("newLogin", "validPass2@", "Mark2", "Sanders");

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "User creation returned error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }
        #endregion

        #region New password
        /// <summary>
        /// Trying to change self password.
        /// </summary>
        [TestMethod]
        public void NewPassword_Self()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_User.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user", Helpers.Users.user_User.id.ToString()),
                    new QueryPart("update"),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("oldpassword", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,
                    query,
                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (!answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log success.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log fail & error.
                            operationResult = false;
                            operationError = "Recived error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to change password of user with higher rank then requeter.
        /// </summary>
        [TestMethod]
        public void NewPassword_ModeratorToAdmin()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_Moderator.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user", Helpers.Users.user_Admin.id.ToString()),
                    new QueryPart("update"),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Unatuorized operation allowed with result: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }

        /// <summary>
        /// Trying to change password of user with lower rank then requester.
        /// </summary>
        [TestMethod]
        public void NewPassword_AdminToUser()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage that data.
                Helpers.Networking.StartPublicServer();

                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", Helpers.Users.user_Admin.tokens[0]),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user", Helpers.Users.user_User.id.ToString()),
                    new QueryPart("update"),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", Helpers.Networking.DefaultQueriesPipeName,

                    query,

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, Query answer) =>
                    {
                        // Is operation success?
                        if (!answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log error.
                            operationResult = true;
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Authorized operation denied with error: " + answer.First.PropertyValueString;
                            operationCompete = true;
                        }
                    });

                // Wait until operation would complete.
                while (!operationCompete)
                {
                    Thread.Sleep(5);
                }

                Assert.IsTrue(operationResult, operationError);
            }
        }
        #endregion
    }
}
