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
using AuthorityController.Data.Personal;

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

            if (!UniformDataOperator.Sql.Attributes.Table.TrySetTables(true, out error,
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
            QueryPart[] query = Helpers.Users.NewUserQuery("admin", "Password123!", "Mark", "Sanders");

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string operationError = null;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

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
                            operationError = "Registration failed. Server answer:" + answerS;
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
            QueryPart[] query = new QueryPart[]
            {
                // TODO FAKE TOKEN
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user", null),
                new QueryPart("logon", null),

                new QueryPart("login", "notexisteduser"),
                new QueryPart("password", "Password123!"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
            };

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

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
                QueryPart.QueryPartsArrayToString(Helpers.Users.NewUserQuery("validuser", "Password123!")),

                // Handler that would recive ther ver answer.
                (PipesProvider.Client.TransmissionLine line, object answer) =>
                {
                    operationCompete = true;
                });

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }

            // Create the query that would simulate logon.
            QueryPart[] query = new QueryPart[]
            {
                // TODO FAKE TOKEN
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user", null),
                new QueryPart("logon", null),

                new QueryPart("login", "validuser"),
                new QueryPart("password", "Password123!"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
            };
            #endregion

            #region Logon
            // Marker that avoid finishing of the test until receiving result.
            operationCompete = false;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

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
                            Assert.IsTrue(true);
                            operationCompete = true;
                        }
                        else
                        {
                            // Log error.
                            Assert.Fail("User not found on server.\nAnswer:" + answerS);
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

            // Create the query that would contain user data.
            QueryPart[] query = Helpers.Users.NewUserQuery("banneduser", "Password123!");

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

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
                            internalError = "Registration failed. Server answer:" + answerS;
                            operationCompete = true;
                        }
                    }
                    else
                    {
                        // Log error.
                        operationResult = false;
                        internalError = "Incorrect format of answer.Required format is string.Type:" + answer.GetType();
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
            User user = (User)Activator.CreateInstance(User.GlobalType);
            user.login = "banneduser";

            // Get user id.
            if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObject(
                User.GlobalType, user, out error,
                new string[] { "userid " },
                "login"))
            {
                return false;
            }

            // Create new permanent ban.
            BanInformation ban = BanInformation.Permanent;
            ban.userId = user.id;
            ban.bannedByUserId = user.id;
            ban.blockedRights = new string[] { "logon" }; // Set logon ban.

            // Set ban to database.
            return UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(
                typeof(BanInformation),
                ban,
                out error);
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
            QueryPart[] query = new QueryPart[]
            {
                // TODO FAKE TOKEN
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user", null),
                new QueryPart("logon", null),

                new QueryPart("login", "banneduser"),
                new QueryPart("password", "Password123!"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
            };

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;
            bool operationResult = false;
            string internalError = null;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(

                // Request connection to localhost server via main pipe.
                "localhost", Helpers.Networking.DefaultQueriesPipeName,

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
                            Assert.IsTrue(true);
                            operationCompete = true;
                            operationResult = true;
                        }
                        else
                        {
                            // Log error.
                            internalError = "Unexisted user found on server.\nAnswer:" + answerS;
                            operationResult = false;
                            operationCompete = true;
                        }
                    }
                    else
                    {
                        // Assert error.
                        internalError = "Incorrect format of answer. Required format is string. Type:" + answer.GetType();
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

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }
    }
}
