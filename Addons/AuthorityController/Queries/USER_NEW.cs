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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Create new user.
    /// 
    /// Storing profile in local dile system by default via UsersLocal API.
    /// Storing profile to SQL server in case if `UniformDataOperator.Sql.SqlOperatorHandler.Active` not null.
    /// </summary>
    public class USER_NEW : IQueryHandler
    {
        /// <summary>
        /// Return the description relative to the lenguage code or default if not found.
        /// </summary>
        /// <param name="cultureKey">Key of target culture.</param>
        /// <returns>Description for relative culture.</returns>
        public virtual string Description(string cultureKey)
        {

            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER NEW\n" +
                            "\tDESCRIPTION: Request creating of new user.\n" +
                            "\tQUERY FORMAT: user=XMLSetializedUser" + UniformQueries.API.SPLITTING_SYMBOL +
                            "new\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="serverTL">Operator that call that operation</param>
        /// <param name="queryParts">Recived query parts.</param>
        public virtual void Execute(object serverTL, QueryPart[] queryParts)
        {
            // Marker that would be mean that some of internal tasks was failed and operation require termination.
            bool failed = false;

            #region Get qyery params
            UniformQueries.API.TryGetParamValue("login", out QueryPart login, queryParts);
            UniformQueries.API.TryGetParamValue("password", out QueryPart password, queryParts);
            UniformQueries.API.TryGetParamValue("fn", out QueryPart firstName, queryParts);
            UniformQueries.API.TryGetParamValue("ln", out QueryPart lastName, queryParts);

            UniformQueries.API.TryGetParamValue("token", out QueryPart token, queryParts);
            UniformQueries.API.TryGetParamValue("guid", out QueryPart guid, queryParts);
            UniformQueries.API.TryGetParamValue("os", out QueryPart os, queryParts);
            UniformQueries.API.TryGetParamValue("mac", out QueryPart mac, queryParts);
            UniformQueries.API.TryGetParamValue("stamp", out QueryPart timeStamp, queryParts);

            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate login
            if (string.IsNullOrEmpty(login.propertyValue) ||
               login.propertyValue.Length < Config.Active.LoginMinSize ||
               login.propertyValue.Length > Config.Active.LoginMaxSize)
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login size. Require " +
                    Config.Active.LoginMinSize + "-" +
                    Config.Active.LoginMaxSize + " caracters.",
                    queryParts);
                return;
            }

            // Check login format.
            if (!Regex.IsMatch(login.propertyValue, @"^[a-zA-Z0-9@._]+$"))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login format. Allowed symbols: [a-z][A-Z][0-9]@._",
                    queryParts);
                return;

            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate password
            if (!API.Validation.PasswordFormat(password.propertyValue, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    queryParts);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate names
            // Validate name.
            if (!API.Validation.NameFormat(ref firstName.propertyValue, out string error) ||
               !API.Validation.NameFormat(ref lastName.propertyValue, out error))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    error,
                    queryParts);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion


            // There you can apply some filter of rood words.

            //-----------------------------------------------

            #region Create user profile data.
            // Create base data.
            User userProfile = (User)Activator.CreateInstance(User.GlobalType);
            userProfile.login = login.propertyValue;
            userProfile.password = SaltContainer.GetHashedPassword(password.propertyValue, Config.Active.Salt);
            userProfile.firstName = firstName.propertyValue;
            userProfile.lastName = lastName.propertyValue;

            // Set rights default rights.
            userProfile.rights = Config.Active.UserDefaultRights;
            #endregion

            #region Data storing
            // Store in SQL data base if provided.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                #region SQL server
                // Start new task for providing possibility to terminate all operation by
                // session's cancelation token. 
                // In other case after termination on any internal task other will started.
                Task.Run(delegate ()
                    {
                        // Set ignorable value to activate autoincrement.
                        userProfile.id = 0;

                        #region Check existing
                        // Virtual user profile that would be used to build the query for exist xhecking.
                        User dbStoredProfile = (User)Activator.CreateInstance(User.GlobalType);

                        // Set login to using in WHERE  sql block.
                        dbStoredProfile.login = login.propertyValue;

                        // Mearker that would contains result of operation.
                        bool userNotExist = false;

                        // Task that would start async waiting for server's answer.
                        Task existingCheckTask = new Task(async delegate ()
                            {
                                // Callback that would be called if data not found.
                                // If not called then data already exist and operation failed.
                                void DataNotFound(object sender, string _)
                                {
                                    // Drop if not target user.
                                    if (!dbStoredProfile.Equals(sender))
                                    {
                                        return;
                                    }

                                    // Unsubscribe.
                                    UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= DataNotFound;

                                    // Enable marker.
                                    userNotExist = true;
                                }

                                // Subscribe on errors.
                                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += DataNotFound;

                                // Set data ro data base.
                                await UniformDataOperator.Sql.SqlOperatorHandler.Active.
                                            SetToObjectAsync(User.GlobalType, Session.Current.TerminationTokenSource.Token, dbStoredProfile,
                                            new string[0],
                                            new string[]
                                            {
                                                "login"
                                            });

                                // Unsubscribe from errors listening.
                                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= DataNotFound;
                            },
                            Session.Current.TerminationTokenSource.Token);
                        existingCheckTask.Start(); // Start async task.

                        // Whait untol result.
                        while (!existingCheckTask.IsCanceled && !existingCheckTask.IsCompleted)
                        {
                            Thread.Sleep(5);
                        }

                        // Drop if user exist
                        if (!userNotExist)
                        {
                            // Inform that user already exist.
                            UniformServer.BaseServer.SendAnswerViaPP(
                                "ERROR: User with login `" + userProfile.login + "` already exist.",
                                queryParts);
                            return;
                        }
                        #endregion

                        #region Create new user
                        // Request creating of new user.
                        Task registrationTask = new Task(
                            async delegate ()
                            {
                                // Subscribe on errors.
                                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += SQLErrorListener;

                                // Set data ro data base.
                                await UniformDataOperator.Sql.SqlOperatorHandler.Active.
                                            SetToTableAsync(User.GlobalType, Session.Current.TerminationTokenSource.Token, userProfile);

                                // If operation nit failed.
                                if (!failed)
                                {
                                    // Request logon.
                                    Logon();

                                    // Unsubscribe from errors listening.
                                    UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorListener;
                                }
                            },
                            Session.Current.TerminationTokenSource.Token);
                        registrationTask.Start();

                    },
                    Session.Current.TerminationTokenSource.Token);
                #endregion
                #endregion
            }
            // Store in local file system.
            else
            {
                #region Local storage
                // Check login exist.
                if (API.LocalUsers.TryToFindUser(login.propertyValue, out User _))
                {
                    // Inform that target user has the same or heigher rank then requester.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Login occupied", queryParts);
                    return;
                }

                // Provide ID.
                userProfile.id = API.LocalUsers.GenerateID(userProfile);

                // Save profile in storage.
                API.LocalUsers.SetProfileAsync(userProfile, Config.Active.UsersStorageDirectory);
                API.LocalUsers.UserProfileStored += LocalDataStoredCallback;
                API.LocalUsers.UserProfileNotStored += LocalDataStroringFailed;
                #endregion
            }
            #endregion

            #region Local callbacks
            // Callback that would be processed in case of success of data storing.
            void LocalDataStoredCallback(User target)
            {
                // Check is that user is a target of this request.
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.LocalUsers.UserProfileStored -= LocalDataStoredCallback;
                    API.LocalUsers.UserProfileNotStored -= LocalDataStroringFailed;

                    Logon();
                }
            }

            // Callback that would be processed in case of fail of data storing.
            void LocalDataStroringFailed(User target, string operationError)
            {
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.LocalUsers.UserProfileStored -= LocalDataStoredCallback;
                    API.LocalUsers.UserProfileNotStored -= LocalDataStroringFailed;

                    // Send answer with operation's error.
                    UniformServer.BaseServer.SendAnswerViaPP(
                        "failed:" + operationError,
                        queryParts);
                }
            }
            #endregion

            #region SQL server callbacks 
            // Looking for user on SQL server if connected.
            void SQLErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!userProfile.Equals(sender))
                {
                    return;
                }

                failed = true;

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, queryParts);
            }
            #endregion

            #region Local methods
            // Request logon with current input data.
            void Logon()
            {
                // Build logon query.
                QueryPart[] logonQuery = new QueryPart[]
                    {
                    new QueryPart("USER", null),
                    new QueryPart("LOGON", null),
                    token,
                    guid,
                    login,
                    password,
                    os,
                    mac,
                    timeStamp,
                    };

                // Create logon subquery.
                foreach (IQueryHandler processor in UniformQueries.API.QueryHandlers)
                {
                    // Fini logon query processor.
                    if (processor is USER_LOGON)
                    {
                        // Execute and send to client token valided to created user.
                        processor.Execute(serverTL, logonQuery);
                        return;
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        /// <returns>Result of comparation.</returns>
        public virtual bool IsTarget(QueryPart[] queryParts)
        {
            // Check token exist.
            if (!UniformQueries.API.QueryParamExist("token", queryParts))
                return false;

            // Check guid exist.
            if (!UniformQueries.API.QueryParamExist("guid", queryParts))
                return false;


            // USER prop.
            if (!UniformQueries.API.QueryParamExist("user", queryParts))
                return false;

            // NEW prop.
            if (!UniformQueries.API.QueryParamExist("new", queryParts))
                return false;


            if (!UniformQueries.API.QueryParamExist("login", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("password", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("fn", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("ln", queryParts))
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
    }
}
