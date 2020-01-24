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
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Creates new user.
    /// 
    /// Stores the profile in a local file system via the <see cref="API.LocalUsers"/>.
    /// Stores the profile on an SQL server in case if <see cref="UniformDataOperator.Sql.SqlOperatorHandler.Active"/> isn't null.
    /// </summary>
    public class USER_NEW : IQueryHandler, IBaseTypeChangable
    {
        /// <summary>
        ///  Type that will be used in operations.
        /// </summary>
        public Type OperatingType { get; set; }

        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public USER_NEW()
        {
            OperatingType = TypeReplacer.GetValidType(typeof(User));
        }
     
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
        /// <param name="query">Recived query.</param>
        public virtual void Execute(object serverTL, Query query)
        {
            // Marker that would be mean that some of internal tasks was failed and operation require termination.
            bool failed = false;

            #region Get query params
            query.TryGetParamValue("login", out QueryPart login);
            query.TryGetParamValue("password", out QueryPart password);
            query.TryGetParamValue("fn", out QueryPart firstName);
            query.TryGetParamValue("ln", out QueryPart lastName);

            query.TryGetParamValue("token", out QueryPart token);
            query.TryGetParamValue("guid", out QueryPart guid);
            query.TryGetParamValue("os", out QueryPart os);
            query.TryGetParamValue("mac", out QueryPart mac);
            query.TryGetParamValue("stamp", out QueryPart timeStamp);

            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate login
            if (string.IsNullOrEmpty(login.PropertyValueString) ||
               login.propertyValue.Length < Config.Active.LoginMinSize ||
               login.propertyValue.Length > Config.Active.LoginMaxSize)
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login size. Require " +
                    Config.Active.LoginMinSize + "-" +
                    Config.Active.LoginMaxSize + " caracters.",
                    query);
                return;
            }

            // Check login format.
            if (!Regex.IsMatch(login.PropertyValueString, @"^[a-zA-Z0-9@._]+$"))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login format. Allowed symbols: [a-z][A-Z][0-9]@._",
                    query);
                return;

            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate password
            if (!API.Validation.PasswordFormat(password.PropertyValueString, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    query);
                return;
            }

            // Can take enough long time so just let other query to process.
            Thread.Sleep(5);
            #endregion

            #region Validate names
            string firstNameString = firstName.PropertyValueString;
            string lastNameString = lastName.PropertyValueString;
            // Validate name.
            if (!API.Validation.NameFormat(ref firstNameString, out string error) ||
               !API.Validation.NameFormat(ref lastNameString, out error))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    error,
                    query);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion


            // There you can apply some filter of rood words.

            //-----------------------------------------------

            #region Create user profile data.
            // Create base data.
            User userProfile = (User)Activator.CreateInstance(OperatingType);
            userProfile.login = login.PropertyValueString;
            userProfile.password = SaltContainer.GetHashedPassword(password.PropertyValueString, Config.Active.Salt);
            userProfile.firstName = firstName.PropertyValueString;
            userProfile.lastName = lastName.PropertyValueString;

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
                        User dbStoredProfile = (User)Activator.CreateInstance(OperatingType);

                        // Set login to using in WHERE  sql block.
                        dbStoredProfile.login = login.PropertyValueString;

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
                                            SetToObjectAsync(
                                                OperatingType, 
                                                Session.Current.TerminationTokenSource.Token, 
                                                dbStoredProfile,
                                                new string[0],
                                                new string[]
                                                {
                                                    "login"
                                                });

                                // Unsubscribe the errors listener.
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
                                query);
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
                                            SetToTableAsync(OperatingType, Session.Current.TerminationTokenSource.Token, userProfile);

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
                if (API.LocalUsers.TryToFindUser(login.PropertyValueString, out User _))
                {
                    // Inform that target user has the same or heigher rank then requester.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Login occupied", query);
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
                        query);
                }
            }
            #endregion

            #region SQL server callbacks 
            // Looking for user on SQL server if connected.
            void SQLErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!userProfile.Equals(sender)) return;

                failed = true;

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, query);
            }
            #endregion

            #region Local methods
            // Request logon with current input data.
            void Logon()
            {
                // Build logon query.
                Query logonQuery = new Query(
                    new QueryPart("USER"),
                    new QueryPart("LOGON"),
                    token,
                    guid,
                    login,
                    password,
                    os,
                    mac,
                    timeStamp
                    );

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
        /// <param name="query">Recived query.</param>
        /// <returns>Result of comparation.</returns>
        public virtual bool IsTarget(Query query)
        {
            // Check token exist.
            if (!query.QueryParamExist("token")) return false;

            // Check guid exist.
            if (!query.QueryParamExist("guid")) return false;

            // USER prop.
            if (!query.QueryParamExist("user")) return false;

            // NEW prop.
            if (!query.QueryParamExist("new")) return false;

            if (!query.QueryParamExist("login")) return false;

            if (!query.QueryParamExist("password")) return false;

            if (!query.QueryParamExist("fn")) return false;

            if (!query.QueryParamExist("ln")) return false;

            // User operation system.
            if (!query.QueryParamExist("os")) return false;

            // Mac adress of logon device.
            if (!query.QueryParamExist("mac")) return false;

            // Session open time
            if (!query.QueryParamExist("stamp")) return false;

            return true;
        }
    }
}
