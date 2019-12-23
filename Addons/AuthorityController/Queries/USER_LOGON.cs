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
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Logon user in system.
    /// Provide token as result.
    /// 
    /// USER&amp;LOGON&amp;login=...&amp;password=...&amp;mac=...&amp;os=....&amp;
    /// </summary>
    public class USER_LOGON : IQueryHandler, IBaseTypeChangable
    {
        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public USER_LOGON()
        {
            OperatingType = TypeReplacer.GetValidType(typeof(User));
            BanInfoOperatingType = TypeReplacer.GetValidType(typeof(BanInformation));
        }

        /// <summary>
        /// A type that will be used in operations.
        /// </summary>
        public Type OperatingType { get; set; }

        /// <summary>
        /// A type that will used for defining ban ifo descriptors. 
        /// </summary>
        public Type BanInfoOperatingType { get; set; }

        #region Query
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
                    return "USER LOGON\n" +
                            "\tDESCRIPTION: Request logon in system by provided entry data.\n" +
                            "\tQUERY FORMAT: " + 
                            string.Format(
                                "LOGIN=userLogin{0}PASSWORD=userPassword{0}OS=operationSystem{0}MAC=macAddress{0}stamp=timeOfQuery\n",
                                "&");
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="serverTL">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public void Execute(object serverTL, Query query)
        {
            bool dataOperationFailed = false;

            #region Input data
            // Get params.
            query.TryGetParamValue("login", out QueryPart login);
            query.TryGetParamValue("password", out QueryPart password);
            query.TryGetParamValue("os", out QueryPart os);
            query.TryGetParamValue("mac", out QueryPart mac);
            query.TryGetParamValue("stamp", out QueryPart timeStamp);

            // Create user instance of requested type.
            User user = (User)Activator.CreateInstance(OperatingType);
            user.login = login.PropertyValueString;
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
                    OperatingType, 
                    Session.Current.TerminationTokenSource.Token,
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
                    BanInfoOperatingType,
                    Session.Current.TerminationTokenSource.Token,
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
                if (!API.LocalUsers.TryToFindUser(login.PropertyValueString, out user))
                {
                    // Inform that user not found.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: User not found", query);
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
            if (!user.IsOpenPasswordCorrect(password.PropertyValueString))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", query);
                return;
            }

            // Check for logon bans
            if(BanInformation.IsBanned(user, "logon"))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: User banned.", query);
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
                mac.PropertyValueString, 
                os.PropertyValueString, 
                timeStamp.PropertyValueString);

            // Set rights.
            Session.Current.SetTokenRights(sessionToken, user.rights);

            // Return session data to user.
            string rightsString = "";
            // Add rights' codes.
            foreach (string rightsCode in user.rights)
            {
                // Add every code splited by '+'.
                rightsString += "+" + rightsCode;
            }

            // Building query.
            Query answerQuery = new Query(
                new QueryPart("token", sessionToken),
                new QueryPart("expiryIn", DateTime.UtcNow.AddMinutes(Config.Active.TokenValidTimeMinutes).ToBinary()),
                new QueryPart("rights", rightsString)
                );

            // Send token to client.
            UniformServer.BaseServer.SendAnswerViaPP(answerQuery, query);
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
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, query);
            }
            #endregion 
        }

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="query">Recived query.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(Query query)
        {
            // Check query.
            if (!query.QueryParamExist("USER"))
                return false;

            // Check query.
            if (!query.QueryParamExist("LOGON"))
                return false;


            // Login for logon.
            if (!query.QueryParamExist("login"))
                return false;

            // Password for logon.
            if (!query.QueryParamExist("password"))
                return false;

            // User operation system.
            if (!query.QueryParamExist("os"))
                return false;

            // Mac adress of logon device.
            if (!query.QueryParamExist("mac"))
                return false;

            // Session open time
            if (!query.QueryParamExist("stamp"))
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
                if(string.IsNullOrEmpty(guestToken))
                {
                    throw new NullReferenceException("Guest token undefined.");
                }

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
                    Session.Current.TerminationTokenSource.Token);
                }
                #endregion

                // Stop if terminated
                if (IsTerminated) return;

                #region Build query
                // Create the query that would simulate logon.
                Query query = new Query(
                    new QueryPart("token", guestToken),
                    new QueryPart("guid", Guid.NewGuid().ToString()),

                    new QueryPart("user"),
                    new QueryPart("logon"),

                    new QueryPart("login", login),
                    new QueryPart("password", password),
                    new QueryPart("os", Environment.OSVersion.VersionString),
                    new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                    new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
                );
                #endregion

                // Request logon.
                UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                    serverIP, pipeName,
                    query,
                    ServerAnswerHandler);
            }

        }
        #endregion
    }
}
