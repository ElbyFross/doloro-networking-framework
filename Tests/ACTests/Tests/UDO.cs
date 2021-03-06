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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniformQueries;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace ACTests.Tests
{
    /// <summary>
    /// Tests that confirms compatibility with UniformDataOperator.
    /// </summary>
    [TestClass]
    public class UDO
    {
        /// <summary>
        /// Set default UDO settigns relative to that tests.
        /// </summary>
        /// <param name="error">An error message in case of occurring. Null if not.</param>
        /// <returns>Result of operation. False if failed.</returns>
        public static bool SetDefaults(out string error)
        {
            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active =
                UniformDataOperator.Sql.MySql.MySqlDataOperator.Active;

            UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.UserId = Local.username;
            UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.Password = Local.password;

            // Create database.
            if (!CreateDB("DNFAuthControl", out error))
            {
                return false;
            }

            // Create tables with overrided path.
            if (!CreateDB("DNFAuthControlOverrideTest", out error))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Trying to create data base by relevant descriptors.
        /// Attention: not remove previous databases from server. Remove manualy before use.
        /// </summary>
        /// <param name="schema">Name of target schema to activation.</param>
        /// <param name="error">An error message in case of occurring. Null if not.</param>
        /// <returns>Result of operation. False if failed.</returns>
        public static bool CreateDB(string schema, out string error)
        {
            if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.ActivateSchema(schema, out error))
            {
                return false;
            }

            if (!UniformDataOperator.Sql.Markup.TableAttribute.TrySetTables(true, out error,
                typeof(User),
                typeof(BanInformation)))
            {
                return false;
            }

            return true;
        }

        [TestInitialize]
        public void Init()
        {
            // Start server that would manage that data.
            Helpers.Networking.StartPublicServer();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Stop all started servers.
            PipesProvider.Server.ServerAPI.StopAllServers();
        }
        
        /// <summary>
        /// Checking creating new user in database.
        /// </summary>
        [TestMethod]
        public void NewUser()
        {
            // Establish operator.
            if (!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }

            // Create query
            // Create the query that would contain user data.
            Query query = Helpers.Users.NewUserQuery("admin", "Password123!", "Mark", "Sanders");

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
                        operationError = "Registration failed. Server answer:" + answer.First.PropertyValueString;
                        operationCompete = true;
                    }
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            Assert.IsTrue(operationResult, operationError);

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }

        /// <summary>
        /// Ckecking logon of user storate in database with invcalid input data.
        /// </summary>
        [TestMethod]
        public void UserLogon_Invalid()
        {
            // Establish operator.
            if (!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }

            error = null;

            // Create the query that would simulate logon.
            Query query = new Query
            (
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user"),
                new QueryPart("logon"),

                new QueryPart("login", "notexisteduser"),
                new QueryPart("password", "Password123!"),
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

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }


        /// <summary>
        /// Ckecking logon of user storate in database with valid input data.
        /// </summary>
        [TestMethod]
        public void UserLogon_Valid()
        {
            // Establish operator.
            if (!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }

            #region Create user.
            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

                // Convert query parts array to string view in correct format provided by UniformQueries API.
                Helpers.Users.NewUserQuery("validuser", "Password123!"),

                // Handler that would recive ther ver answer.
                (PipesProvider.Client.TransmissionLine line, Query answer) =>
                {
                    operationCompete = true;
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            // Create the query that would simulate logon.
            Query query = new Query
            (
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user"),
                new QueryPart("logon"),

                new QueryPart("login", "validuser"),
                new QueryPart("password", "Password123!"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
            );
            #endregion

            #region Logon
            // Marker that avoid finishing of the test until receiving result.
            operationCompete = false;

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
                        Assert.IsTrue(true);
                        operationCompete = true;
                    }
                    else
                    {
                        // Log error.
                        Assert.Fail("User not found on server.\nAnswer:" + answer.First.PropertyValueString);
                        operationCompete = true;
                    }
                });
            #endregion

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }

        /// <summary>
        /// Trying to ban user in database.
        /// </summary>
        [TestMethod]
        public void UserBan()
        {
            // Establish operator.
            if (!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }

            if (!CreateUserForBan(out error))
            {
                Assert.Fail(error);
                return;
            }

            if (!BanUser(out error))
            {
                Assert.Fail(error);
                return;
            }

            if (!TryToLogonAsBannedUser(out error))
            {
                Assert.Fail(error);
                return;
            }

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }

        /// <summary>
        /// Appling ban to user.
        /// </summary>
        /// <param name="error">Error messaage if occured.</param>
        /// <returns>Result of operation.</returns>
        public bool CreateUserForBan(out string error)
        {
            string internalError = null;
            bool operationCompete = false;
            bool operationResult = false;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,
                
                // Create the query that would contain user data.
                Helpers.Users.NewUserQuery("banneduser", "Password123!"),

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
                        internalError = "Registration failed. Server answer:" + answer.First.PropertyValueString;
                        operationCompete = true;
                    }
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            error = internalError;
            return operationResult;
        }

        /// <summary>
        /// Bans created user.
        /// </summary>
        /// <param name="error">Error messaage if occured.</param>
        /// <returns>Result of operation.</returns>
        public bool BanUser(out string error)
        {
            // Create users for test.
            Helpers.Users.SetBaseUsersPool();

            // Create admin user.
            if(!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(typeof(User),
                Helpers.Users.user_Moderator, out error))
            {
                return false;
            }

            // Building query.
            BanInformation banInformation = BanInformation.Permanent;
            banInformation.blockedRights = new string[] { "logon" };
            Query query = new Query(
                new QueryPart("token", Helpers.Users.user_Moderator.tokens[0]),
                new QueryPart("guid", "bunUserTest"),
                new QueryPart("user", "banneduser"),
                new QueryPart("ban", banInformation));

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string internalError = null;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,
                query,
                // Handler that would recive ther ver answer.
                (PipesProvider.Client.TransmissionLine line, Query answer) =>
                {
                    if(answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        internalError = query.First.PropertyValueString;
                        operationResult = false;
                    }
                    else
                    {
                        operationResult = true;
                    }
                    operationCompete = true;
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            error = internalError;
            return operationResult;

            //User user = (User)Activator.CreateInstance(User.GlobalType);
            //user.login = "banneduser";

            //// Get user id.
            //if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObject(
            //    User.GlobalType, user, out error,
            //    new string[] { "userid " },
            //    "login"))
            //{
            //    return false;
            //}

            //// Create new permanent ban.
            //BanInformation ban = BanInformation.Permanent;
            //ban.userId = user.id;
            //ban.bannedByUserId = user.id;
            //ban.blockedRights = new string[] { "logon" }; // Set logon ban.

            //// Set ban to database.
            //return UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(
            //    typeof(BanInformation),
            //    ban,
            //    out error);
        }

        /// <summary>
        /// Trying to logon via banned user.
        /// </summary>
        /// <param name="error">Error messaage if occured.</param>
        /// <returns>Result of operation.</returns>
        public bool TryToLogonAsBannedUser(out string error)
        {
            error = null;

            // Create the query that would simulate logon.
            Query query = new Query
            (
                new QueryPart("token", Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user"),
                new QueryPart("logon"),

                new QueryPart("login", "banneduser"),
                new QueryPart("password", "Password123!"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
            );

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string internalError = null;

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
                        Assert.IsTrue(true);
                        operationCompete = true;
                        operationResult = true;
                    }
                    else
                    {
                        // Log error.
                        internalError = "Unexisted user found on server.\nAnswer:" + answer.First.PropertyValueString;
                        operationResult = false;
                        operationCompete = true;
                    }
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            error = internalError;
            return operationResult;
        }

        /// <summary>
        /// Trying to change password for user in database.
        /// </summary>
        [TestMethod]
        public void UserSetPassword()
        {
            // Establish operator.
            if (!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }


            var user_Admin = new User()
            {
                login = "adminForBan",
                password = AuthorityController.Data.Application.SaltContainer.GetHashedPassword(
                    "password", AuthorityController.Data.Application.Config.Active.Salt),
                tokens = new System.Collections.Generic.List<string>
                (new string[] { Tokens.UnusedToken }),
                rights = new string[]{
                    "rank=8",
                    "banhammer",
                    "passwordManaging" }
            };

            user_Admin.id = AuthorityController.API.LocalUsers.GenerateID(user_Admin);

            // Admin
            AuthorityController.Session.Current.AssignTokenToUser(user_Admin, user_Admin.tokens[0]);
            AuthorityController.Session.Current.SetTokenRights(user_Admin.tokens[0], user_Admin.rights);

            // Create admin user.
            if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(typeof(User),
                user_Admin, out error))
            {
                Assert.Fail(error);
                return;
            }

            // Creating an update query.
            Query query = new Query(
                new QueryPart("token", user_Admin.tokens[0]),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user", user_Admin.login),
                new QueryPart("update"),

                new QueryPart("password", "newPassword!2"),
                new QueryPart("oldpassword", "password"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
            );

            // A markers that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string operationError = null;

            // Starts reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,
                query,
                // A handler that will recive an answer from the server.
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

        /// <summary>
        /// Validates the <see cref="AuthorityController.Queries.SET_TOKEN_RIGHTS"/> query.
        /// </summary>
        [TestMethod]
        public void SetTokenRights()
        {
            if(!SetDefaults(out string error))
            {
                Assert.Fail(error);
                return;
            }

            #region Users genration
            User moderatedUser = new User
            {
                id = 213455,
                login = "mu",
                password = SaltContainer.GetHashedPassword("pass", Config.Active.Salt),
                firstName = "mu",
                rights = new string[] { "rank=2", "1", "2" }
            };
            moderatedUser.tokens.Add(Tokens.UnusedToken);
            
            User adminUser = new User
            {
                id = 213456,
                login = "admFRT",
                password = SaltContainer.GetHashedPassword("pass", Config.Active.Salt),
                firstName = "admFRT",
                rights = new string[] { "rank=8", "banhammer" }
            };
            adminUser.tokens.Add(Tokens.UnusedToken);
            #endregion

            #region Sending the data to server.
            // Create a user for moderation.
            if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(typeof(User),
                moderatedUser, out error))
            {
                Assert.Fail(error);
                return;
            }

            // Create admin user.
            if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(typeof(User),
                adminUser, out error))
            {
                Assert.Fail(error);
                return;
            }
            #endregion

            #region Providing tokens
            AuthorityController.Session.Current.AssignTokenToUser
                (moderatedUser, moderatedUser.tokens[0]);

            AuthorityController.Session.Current.AssignTokenToUser
                (adminUser, adminUser.tokens[0]);
            #endregion

            #region SetToken rights
            AuthorityController.Session.Current.SetTokenRights
                (moderatedUser.tokens[0], moderatedUser.rights);

            AuthorityController.Session.Current.SetTokenRights
                (adminUser.tokens[0], adminUser.rights);
            #endregion

            // Building the query.
            var query = new Query(
                new QueryPart("set"),
                new QueryPart("targetToken", moderatedUser.tokens[0]),
                new QueryPart("rights", "rank=4+a+b+c" ),
                new QueryPart("set"),
                new QueryPart("token", adminUser.tokens[0]),
                new QueryPart("guid", "tokenRightsUpdateTest"));

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string internalError = null;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,
                query,
                // Handler that would recive ther ver answer.
                (PipesProvider.Client.TransmissionLine line, Query answer) =>
                {
                    if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        internalError = query.First.PropertyValueString;
                        operationResult = false;
                    }
                    else
                    {
                        operationResult = true;
                    }
                    operationCompete = true;
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            Assert.IsTrue(operationResult, internalError);
        }
    }
}
