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
using AuthorityControllerTests;
using Microsoft.Win32.SafeHandles;
using UniformQueries;
using UniformServer;
using UniformClient;
using PipesProvider.Server.TransmissionControllers;
using AuthorityController.Data;

namespace AuthorityController.Tests
{
    [TestClass]
    public class Queries
    {
        /// <summary>
        /// Name  of the pipe that would be started on server during tests.
        /// </summary>
        readonly string PIPE_NAME = "ACTestPublic";

        #region Users
        User user_SuperAdmin = null;
        User user_Admin = null;
        User user_Moderator = null;
        User user_PrivilegedUser = null;
        User user_User = null;
        User user_Guest = null;
        #endregion

        /// <summary>
        /// Starting public server that would able to recive queries.
        /// </summary>
        public void StartPublicServer()
        {
            // Stop previos servers.
            PipesProvider.Server.ServerAPI.StopAllServers();

            // Start new server pipe.
            AuthorityTestServer.Server.StartQueryProcessing(PIPE_NAME);
        }

        /// <summary>
        /// Creating and apply base users pool:
        /// -Super admin
        /// -Admin
        /// -Moderator
        /// -Privileged user
        /// -User
        /// -Guest
        /// </summary>
        public void SetBaseUsersPool()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Set new test directory to avoid conflicts with users profiles.
                Config.Active.UsersStorageDirectory = "Tests\\Queries\\Users\\" + Guid.NewGuid().ToString() + "\\";

                // Clear current user pool.
                AuthorityController.API.Users.ClearUsersLoadedData();

                #region Create superadmin
                user_SuperAdmin = new User()
                {
                    id = 1,
                    login = "sadmin",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=16",
                    "bannhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_SuperAdmin.id = API.Users.GenerateID(user_SuperAdmin);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_SuperAdmin, Config.Active.UsersStorageDirectory);

                #endregion

                #region Create admin
                user_Admin = new User()
                {
                    login = "admin",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=8",
                    "bannhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_Admin.id = API.Users.GenerateID(user_Admin);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_Admin, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create moderator
                user_Moderator = new User()
                {
                    login = "moderator",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=4",
                    "bannhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_Moderator.id = API.Users.GenerateID(user_Moderator);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_Moderator, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create privileged user
                user_PrivilegedUser = new User()
                {
                    login = "puser",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=2",
                    "passwordManaging" }
                };

                // Generate ID.
                user_PrivilegedUser.id = API.Users.GenerateID(user_PrivilegedUser);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_PrivilegedUser, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create user
                user_User = new User()
                {
                    login = "user",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=1",
                    "passwordManaging" }
                };

                // Generate ID.
                user_User.id = API.Users.GenerateID(user_User);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_User, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create guest
                user_Guest = new User()
                {
                    login = "guest",
                    password = API.Users.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { AuthorityController.API.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=0"}
                };

                // Generate ID.
                user_Guest.id = API.Users.GenerateID(user_Guest);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user_Guest, Config.Active.UsersStorageDirectory);
                #endregion

                // Wait until loading.
                while (API.Users.HasAsyncLoadings)
                {
                    Thread.Sleep(5);
                }

                #region Authorize tokens
                // Super admin
                AuthorityController.Session.Current.AsignTokenToUser(user_SuperAdmin, user_SuperAdmin.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_SuperAdmin.tokens[0], user_SuperAdmin.rights);

                // Admin
                AuthorityController.Session.Current.AsignTokenToUser(user_Admin, user_Admin.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Admin.tokens[0], user_Admin.rights);

                // Moderator
                AuthorityController.Session.Current.AsignTokenToUser(user_Moderator, user_Moderator.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Moderator.tokens[0], user_Moderator.rights);

                // Privileged user
                AuthorityController.Session.Current.AsignTokenToUser(user_PrivilegedUser, user_PrivilegedUser.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_PrivilegedUser.tokens[0], user_PrivilegedUser.rights);

                // User
                AuthorityController.Session.Current.AsignTokenToUser(user_User, user_User.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_User.tokens[0], user_User.rights);

                // Guest
                AuthorityController.Session.Current.AsignTokenToUser(user_Guest, user_Guest.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Guest.tokens[0], user_Guest.rights);
                #endregion

                // Start server that would manage that data.
                StartPublicServer();
            }
        }

        [TestInitialize]
        public void Setup()
        {
            // Start broadcasting server that would share guest tokens.
            BaseServer.StartBroadcastingViaPP(
                "guests",
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);
        }

        /// <summary>
        /// Tryinging to get guest token using query.
        /// </summary>
        [TestMethod]
        public void GetGuestToken()
        {
            // Marker.
            bool waitingAnswer = true;

            #region Server answer processing
            // Start listening client.
            UniformClient.Standard.SimpleClient.ReciveAnonymousBroadcastMessage(
                "localhost", "guests",
                (PipesProvider.Client.TransmissionLine line, object obj) =>
                {
                    // Validate answer.
                    if (obj is string answer)
                    {
                        QueryPart[] recivedQuery = UniformQueries.API.DetectQueryParts(answer);

                        // Check token.
                        if(UniformQueries.API.TryGetParamValue("token", out QueryPart token, recivedQuery))
                        {
                            // Set token as GUEST to share between other tests.
                            Configurator.GUEST_TOKEN = token.propertyValue;

                            // Assert test result.
                            bool tokenProvided = !string.IsNullOrEmpty(token.propertyValue);
                            Assert.IsTrue(tokenProvided, "Token is null.\n"+ answer);
                        }
                        else
                        {
                            // Inform that failed.
                            Assert.IsTrue(false, "Token not provided.\n" + answer);
                        }

                        // Check expire time.
                        if (!UniformQueries.API.TryGetParamValue("expiryIn", out QueryPart _, recivedQuery))
                            Assert.IsTrue(false, "Expire time not provided.\n" + answer);

                        // Check rights providing.
                        if (!UniformQueries.API.TryGetParamValue("rights", out QueryPart _, recivedQuery))
                            Assert.IsTrue(false, "Rights not shared.\n" + answer);
                    }
                    else
                    {
                        // Inform that failed.
                        Assert.IsTrue(false, "Incorrect answer type.");
                    }

                    // Unlock finish blocker.
                    waitingAnswer = false;
                });
            #endregion

            // Wait server answer.
            while(waitingAnswer)
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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", user_User.tokens[0]),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("set"),
                    new QueryPart("targetToken", user_Admin.tokens[0]),
                    new QueryPart("rights", "newRight"),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "Unathorized operation passed. Server answer:" + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", user_Admin.tokens[0]),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("set"),
                    new QueryPart("targetToken", user_User.tokens[0]),
                    new QueryPart("rights", "newRight"),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (!answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "Operation failed with error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        /// Trying to ban user but has no rights to this.
        /// </summary>
        [TestMethod]
        public void UserBan_NoRights()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();
            }
        }

        /// <summary>
        /// Trying to ban user that has a higher rank than requester.
        /// </summary>
        [TestMethod]
        public void UserBan_HighrankerBan()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();
            }
        }

        /// <summary>
        /// Trying to ban user with enough rights to that.
        /// </summary>
        [TestMethod]
        public void UserBan_HasRights()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();
            }
        }

        /// <summary>
        /// Check ban expiring.
        /// </summary>
        [TestMethod]
        public void UserBan_BanExpire()
        {

        }


        /// <summary>
        /// Trying to logon as existed user with corerct logon data.
        /// </summary>
        [TestMethod]
        public void Logon_ValidData()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", AuthorityController.API.Tokens.UnusedToken),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", null),
                    new QueryPart("logon", null),

                    new QueryPart("login", "sadmin"),
                    new QueryPart("password", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                        // Is operation success?
                        if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                            // Log error.
                            Assert.Fail("Recived error:\n" + answerS);
                                operationCompete = true;
                            }
                            else
                            {
                            // Trying to get toekn from answer.
                            if (UniformQueries.API.TryGetParamValue("token", out string value, answerS))
                                {
                                // Confirm logon.
                                Assert.IsTrue(true);
                                    operationCompete = true;
                                }
                                else
                                {
                                // Log error.
                                Assert.Fail("Answer not contain token:\nFull answer:" + answerS);
                                    operationCompete = true;
                                }
                            }
                        }
                        else
                        {
                        // Assert error.
                        Assert.Fail("Incorrect format of answer. Required format is string. Type:" + answer.GetType());
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
        /// Trying to logon as existed user that not registred.
        /// </summary>
        [TestMethod]
        public void Logon_UserNotExist()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", AuthorityController.API.Tokens.UnusedToken),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", null),
                    new QueryPart("logon", null),

                    new QueryPart("login", "notExistedUser"),
                    new QueryPart("password", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                Assert.IsTrue(true);
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                Assert.Fail("Unexisted user found on server.\nAnswer:" + answerS);
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Assert error.
                            Assert.Fail("Incorrect format of answer. Required format is string. Type:" + answer.GetType());
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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", AuthorityController.API.Tokens.UnusedToken),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", null),
                    new QueryPart("logon", null),

                    new QueryPart("login", "user"),
                    new QueryPart("password", "invalidPassword"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                    
                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                Assert.IsTrue(true);
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                Assert.Fail("Unexisted user found on server.\nAnswer:" + answerS);
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Assert error.
                            Assert.Fail("Incorrect format of answer. Required format is string. Type:" + answer.GetType());
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
        /// Trying to logoff user by invalid token.
        /// </summary>
        [TestMethod]
        public void Logoff_InvalidToken()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Generate new token.
                string newToken = AuthorityController.API.Tokens.UnusedToken;

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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Get token of registred user.
                string userToken = user_User.tokens[0];

                // Logoff unregistred token.
                bool result = AuthorityController.Queries.USER_LOGOFF.LogoffToken(userToken);

                // Assert that token was rejected by a system.
                // If token was processed that this mean that system failed.
                Assert.IsTrue(result, "Token not detected.");
            }
        }

        /// <summary>
        /// Trying to create user with valid data.
        /// </summary>
        [TestMethod]
        public void NewUser_ValidData()
        {
            #region Getting guest token to logon on server
            if (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                StartPublicServer();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", Configurator.GUEST_TOKEN),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user"),
                    new QueryPart("new"),

                    new QueryPart("login", "login123@"),
                    new QueryPart("password", "Password123!"),
                    new QueryPart("fn", "Mark"),
                    new QueryPart("sn", "Sanders"),

                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (!answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "User creation returned error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        public void NewUser_InvalidPassword()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion
            
            lock (Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                StartPublicServer();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", Configurator.GUEST_TOKEN),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user"),
                    new QueryPart("new"),

                    new QueryPart("login", "login123@"),
                    new QueryPart("password", "aa!"),
                    new QueryPart("fn", "Mark"),
                    new QueryPart("sn", "Sanders"),

                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "User creation returned error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        public void NewUser_InvalidLogin()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion
            
            lock (Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                StartPublicServer();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", Configurator.GUEST_TOKEN),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user"),
                    new QueryPart("new"),

                    new QueryPart("login", "a"),
                    new QueryPart("password", "validPass2@"),
                    new QueryPart("fn", "Mark"),
                    new QueryPart("sn", "Sanders"),

                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "User creation returned error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        public void NewUser_UserExist()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion

            lock (Locks.CONFIG_LOCK)
            {
                // Create users.
                SetBaseUsersPool();

                // Start new server for thsi test to avoid conflicts.
                StartPublicServer();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", Configurator.GUEST_TOKEN),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user"),
                    new QueryPart("new"),

                    new QueryPart("login", "admin"),
                    new QueryPart("password", "validPass2@"),
                    new QueryPart("fn", "Mark"),
                    new QueryPart("sn", "Sanders"),

                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "User creation returned error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        public void NewUser_InvalidPersonal()
        {
            #region Getting guset token to logon on server
            if (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
            {
                // Request guest token.
                GetGuestToken();

                // Wait until guest token would provided.
                while (string.IsNullOrEmpty(Configurator.GUEST_TOKEN))
                {
                    Thread.Sleep(5);
                }
            }
            #endregion
            
            lock (Locks.CONFIG_LOCK)
            {
                // Start new server for thsi test to avoid conflicts.
                StartPublicServer();

                // Create the query that would contain user data.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", Configurator.GUEST_TOKEN),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user"),
                    new QueryPart("new"),

                    new QueryPart("login", "newLogin"),
                    new QueryPart("password", "validPass2@"),
                    new QueryPart("fn", "Mark2"),
                    new QueryPart("sn", "Sanders"),

                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "User creation returned error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
        /// Trying to change self password.
        /// </summary>
        [TestMethod]
        public void NewPassword_Self()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", user_User.tokens[0]),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", user_User.id.ToString()),
                    new QueryPart("new", null),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("oldpassword", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (!answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log success.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log fail & error.
                                operationResult = false;
                                operationError = "Recived error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log fail & error.
                            operationResult = false;
                            operationError = "Incorrect format of answer. Required format is string. Type:" + answer.GetType();
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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", user_Moderator.tokens[0]),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", user_Admin.id.ToString()),
                    new QueryPart("new", null),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("oldpassword", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "Unatuorized operation allowed with result: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
            lock (Locks.CONFIG_LOCK)
            {
                // Create users for test.
                SetBaseUsersPool();

                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", user_Admin.tokens[0]),
                    new QueryPart("guid", AuthorityController.API.Tokens.UnusedToken),

                    new QueryPart("user", user_User.id.ToString()),
                    new QueryPart("new", null),

                    new QueryPart("password", "newPassword!2"),
                    new QueryPart("oldpassword", "password"),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", "anonymous"),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };

                // Marker that avoid finishing of the test until receiving result.
                bool operationCompete = false;
                bool operationResult = false;
                string operationError = null;

                // Start reciving clent line.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                    // Request connection to localhost server via main pipe.
                    "localhost", PIPE_NAME,

                    // Convert query parts array to string view in correct format provided by UniformQueries API.
                    QueryPart.QueryPartsArrayToString(query),

                    // Handler that would recive ther ver answer.
                    (PipesProvider.Client.TransmissionLine line, object answer) =>
                    {
                        // Trying to convert answer to string
                        if (answer is string answerS)
                        {
                            // Is operation success?
                            if (!answerS.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                            {
                                // Log error.
                                operationResult = true;
                                operationCompete = true;
                            }
                            else
                            {
                                // Log error.
                                operationResult = false;
                                operationError = "Authorized operation denied with error: " + answerS;
                                operationCompete = true;
                            }
                        }
                        else
                        {
                            // Log error.
                            operationResult = false;
                            operationError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
    }
}
