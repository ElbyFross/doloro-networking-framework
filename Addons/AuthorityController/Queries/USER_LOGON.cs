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
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Logon user in system.
    /// Provide token as result.
    /// 
    /// USER&LOGON&login=...&password=...&mac=...&os=....&
    /// </summary>
    public class USER_LOGON : IQueryHandler
    {
        #region Query
        public string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER LOGON\n" +
                            "\tDESCRIPTION: Request logon in system by provided entry data.\n" +
                            "\tQUERY FORMAT: " + 
                            string.Format(
                                "LOGIN=userLogin{0}PASSWORD=userPassword{0}OS=operationSystem{0}MAC=macAddress{0}stamp=timeOfQuery\n",
                            UniformQueries.API.SPLITTING_SYMBOL);
            }
        }

        public void Execute(QueryPart[] queryParts)
        {
            bool dataOperationFailed = false;

            #region Input data
            // Get params.
            UniformQueries.API.TryGetParamValue("login", out QueryPart login, queryParts);
            UniformQueries.API.TryGetParamValue("password", out QueryPart password, queryParts);
            UniformQueries.API.TryGetParamValue("os", out QueryPart os, queryParts);
            UniformQueries.API.TryGetParamValue("mac", out QueryPart mac, queryParts);
            UniformQueries.API.TryGetParamValue("stamp", out QueryPart timeStamp, queryParts);

            // Create user instance of requested type.
            User user = (User)Activator.CreateInstance(User.GlobalType);
            user.login = login.propertyValue;
            #endregion

            Task asyncDataOperator = null;                       
            // Get data from SQL server if connected.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                #region SQL server      

                #region Get user
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Request data.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObjectAsync(
                    User.GlobalType, 
                    Session.Current.TerminationToken,
                    user, 
                    new string[0],
                    new string[] { "login" });
                
                // If async operation started.
                if (asyncDataOperator != null)
                {
                    // Wait until finishing.
                    while (!asyncDataOperator.IsCompleted && !asyncDataOperator.IsCanceled)
                    {
                        Thread.Sleep(5);
                    }

                    // Unsubscribe from errors listening.
                    UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;
                }

                // Drop if async operation failed.
                if (dataOperationFailed)
                {
                    return;
                }
                #endregion

                #region Get bans data
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Request data.
                Task banListnerAsyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObjectsAsync(
                    typeof(BanInformation),
                    Session.Current.TerminationToken,
                    new BanInformation() { userId = user.id },
                    delegate(IList collection)
                    {
                        user.bans = (List<BanInformation>)collection;
                    },
                    new string[0],
                    new string[] { "user_userid" });

                // If async operation started.
                if (banListnerAsyncDataOperator != null)
                {
                    // Wait until finishing.
                    while (!banListnerAsyncDataOperator.IsCompleted && !banListnerAsyncDataOperator.IsCanceled)
                    {
                        Thread.Sleep(5);
                    }

                    // Unsubscribe from errors listening.
                    UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;
                }
                
                #endregion

                #endregion
            }
            // Looking for user in local storage.
            else
            {
                #region Local storage
                // Find user.
                if (!API.LocalUsers.TryToFindUser(login.propertyValue, out user))
                {
                    // Inform that user not found.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: User not found", queryParts);
                    return;
                }
                #endregion
            }

            // Drop if async operation failed.
            if (dataOperationFailed)
            {
                return;
            }

            #region Validate password.
            // Comapre password with stored.
            if (!user.IsOpenPasswordCorrect(password.propertyValue))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", queryParts);
                return;
            }

            // Check for logon bans
            if(BanInformation.IsBanned(user, "logon"))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: User banned.", queryParts);
                return;
            }
            #endregion

            #region Build answer
            // Generate new token.
            string sessionToken = UniformQueries.Tokens.UnusedToken;

            // Registrate token in session.
            if (!user.tokens.Contains(sessionToken))
            {
                user.tokens.Add(sessionToken);
            }
            // Registrate token for user.
            Session.Current.AsignTokenToUser(
                user, 
                sessionToken,
                mac.propertyValue, 
                os.propertyValue, 
                timeStamp.propertyValue);

            // Set rights.
            Session.Current.SetTokenRights(sessionToken, user.rights);

            // Return session data to user.
            string query = string.Format("token={1}{0}expiryIn={2}{0}rights=",
                UniformQueries.API.SPLITTING_SYMBOL,
                sessionToken,
                Config.Active.TokenValidTimeMinutes);

            // Add rights' codes.
            foreach(string rightsCode in user.rights)
            {
                // Add every code splited by '+'.
                query += "+" + rightsCode;
            }

            // Send token to client.
            UniformServer.BaseServer.SendAnswerViaPP(query, queryParts);
            #endregion

            #region SQL server callbacks
            // Looking for user on SQL server if connected.
            void ErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!user.Equals(sender))
                {
                    return;
                }

                // Mark that data receiving failed.
                dataOperationFailed = true;

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, queryParts);
            }
            #endregion 
        }

        public bool IsTarget(QueryPart[] queryParts)
        {
            // Check query.
            if (!UniformQueries.API.QueryParamExist("USER", queryParts))
                return false;

            // Check query.
            if (!UniformQueries.API.QueryParamExist("LOGON", queryParts))
                return false;


            // Login for logon.
            if (!UniformQueries.API.QueryParamExist("login", queryParts))
                return false;

            // Password for logon.
            if (!UniformQueries.API.QueryParamExist("password", queryParts))
                return false;

            // User operation system.
            if (!UniformQueries.API.QueryParamExist("os", queryParts))
                return false;

            // Mac adress of logon device.
            if (!UniformQueries.API.QueryParamExist("mac", queryParts))
                return false;

            // Session open time
            if (!UniformQueries.API.QueryParamExist("stamp", queryParts))
                return false;

            return true;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Handler that provide logogn process.
        /// </summary>
        public class LogonProcessor : UniformQueries.Executable.Security.AuthQueryProcessor
        {
            /// <summary>
            /// Logon on routing target server.
            /// </summary>
            /// <param name="guestToken">Token that would be able to get guest access on target server.</param>
            /// <param name="login">User's login that would impersonate this server on related server.</param>
            /// <param name="password">User's password.</param>
            /// <param name="serverIP">IP address of name of server.</param>
            /// <param name="pipeName">Named pipe that started on server.</param>
            public async void TryToLogonAsync(
                string guestToken,
                string login,
                string password,
                string serverIP,
                string pipeName)
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
                    Session.Current.TerminationToken);
                }
                #endregion

                // Stop if terminated
                if (IsTerminated) return;

                #region Build query
                // Create the query that would simulate logon.
                QueryPart[] query = new QueryPart[]
                {
                    new QueryPart("token", guestToken),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user", null),
                    new QueryPart("logon", null),

                    new QueryPart("login", login),
                    new QueryPart("password", password),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
                };
                #endregion

                // Request logon.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                    serverIP, pipeName,
                    QueryPart.QueryPartsArrayToString(query),
                    ServerAnswerHandler
                    );
            }

        }
        #endregion
    }
}
