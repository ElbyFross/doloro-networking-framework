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
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Set new password for user.
    /// Require admin or certen user rights.
    /// </summary>
    public class USER_UPDATE_PASSWORD : IQueryHandler , UniformDataOperator.Modifiers.IBaseTypeChangable
    {
        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public USER_UPDATE_PASSWORD()
        {
            OperatingType = UniformDataOperator.Modifiers.TypeReplacer.GetValidType(typeof(User));
        }

        /// <summary>
        ///  Type that will be used in operations.
        /// </summary>
        public Type OperatingType { get; set; }

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
                    return "USER UPDATE PASSWORD\n" +
                            "\tDESCRIPTION: Request new password for user." +
                            "\n\tToken confirm rights to change it.\n" +
                            "\n\tOld password required to avoid access from public places.\n" +
                            "\tQUERY FORMAT: user=..." + UniformQueries.API.SPLITTING_SYMBOL + 
                            "update" + UniformQueries.API.SPLITTING_SYMBOL +
                            "password=..." + UniformQueries.API.SPLITTING_SYMBOL + 
                            "oldPassword=..." + UniformQueries.API.SPLITTING_SYMBOL +
                            "token=..." + "\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="serverTL">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public virtual void Execute(object serverTL, Query query)
        {
            // Init defaults.
            Task asyncDataOperator = null;
            bool dataOperationFailed = false;

            #region Get params.
            query.TryGetParamValue("password", out QueryPart password);
            query.TryGetParamValue("oldPassword", out QueryPart oldPassword);
            #endregion

            // Validate user rights to prevent not restricted acess passing.
            if(!Handler.ValidateUserRights(
                OperatingType,
                query,
                Config.Active.QUERY_UserNewPassword_RIGHTS, 
                out string error,
                out User userProfile))
            {
                // Drop if invalid. 
                return;
            }
            
            #region Validate password.
            // Comapre password with stored.
            if (!userProfile.IsOpenPasswordCorrect(oldPassword.PropertyValueString))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", query);
                return;
            }
            #endregion

            #region Validate new password
            if (!API.Validation.PasswordFormat(password.PropertyValueString, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    query);
                return;
            }
            #endregion

            // Update password.
            userProfile.password = SaltContainer.GetHashedPassword(password.PropertyValueString, Config.Active.Salt);

            // Update stored profile.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Update on SQL server.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTableAsync(
                    OperatingType,
                    Session.Current.TerminationTokenSource.Token,
                    userProfile);
            }
            else
            {
                // Ipdate in local storage.
                API.LocalUsers.SetProfile(userProfile);
            }

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
                asyncDataOperator = null;
            }

            // Log sucess if not failed.
            if (!dataOperationFailed)
            {
                // Inform about success
                UniformServer.BaseServer.SendAnswerViaPP(
                    "success",
                    query);
            }

            #region SQL server callbacks
            // Looking for user on SQL server if connected.
            void ErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!userProfile.Equals(sender))
                {
                    return;
                }

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, query);
                dataOperationFailed = true;
            }
            #endregion 
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="serverTL">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public virtual void Execute2(object serverTL, Query query)
        {
            bool dataOperationFailed = false;
            string error = null;

            #region Get params.
            query.TryGetParamValue("user", out QueryPart user);
            query.TryGetParamValue("password", out QueryPart password);
            query.TryGetParamValue("oldPassword", out QueryPart oldPassword);
            query.TryGetParamValue("token", out QueryPart token);
            #endregion

            User userProfile = (User)Activator.CreateInstance(OperatingType);
            userProfile.login = user.PropertyValueString;

            #region Detect target user
            Task asyncDataOperator = null;                       
            // Get data from SQL server if connected.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                #region SQL server                
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Request data.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObjectAsync(
                    OperatingType,
                    Session.Current.TerminationTokenSource.Token,
                    userProfile,
                    new string[0],
                    new string[] { "login" });
                #endregion
            }
            // Looking for user in local storage.
            else
            {
                #region Local storage
                if (!API.LocalUsers.TryToFindUserUniform(user.PropertyValueString, out userProfile, out error))
                {
                    // Inform about error.
                    UniformServer.BaseServer.SendAnswerViaPP(error, query);
                    return;
                }
                #endregion
            }
            #endregion

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
                asyncDataOperator = null;
            }

            #region Check base requester rights
            if (!API.Tokens.IsHasEnoughRigths(
                token.PropertyValueString,
                out Data.Temporal.TokenInfo tokenInfo,
                out error,
                Config.Active.QUERY_UserNewPassword_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, query);
                return;
            }
            #endregion

            #region Check rank permition
            // Is that the self update?
            bool isSelfUpdate = false;

            // Check every token provided to target user.
            foreach(string userToken in userProfile.tokens)
            {
                // Comare tokens.
                if (token.PropertyValueString == userToken)
                {
                    // Mark as self target.
                    isSelfUpdate = true;

                    // Interupt loop.
                    break;
                }
            }

            // If not the self update request, then check rights to moderate.
            if (!isSelfUpdate)
            {
                // Get target User's rank.
                if(!API.Collections.TryGetPropertyValue("rank", out string userRank, userProfile.rights))
                {
                    // Inform that rights not enough.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: User rank not defined", query);
                    return;
                }

                // Check token rights.
                if (!API.Collections.IsHasEnoughRigths(tokenInfo.rights,
                    // Request hiegher rank then user and at least moderator level.
                    ">rank=" + userRank, ">rank=2"))
                {
                    // Inform that rank not defined.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", query);
                    return;
                }
            }
            #endregion

            #region Validate password.
            // Comapre password with stored.
            if (!userProfile.IsOpenPasswordCorrect(oldPassword.PropertyValueString))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", query);
                return;
            }
            #endregion

            #region Validate new password
            if(!API.Validation.PasswordFormat(password.PropertyValueString, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    query);
                return;
            }
            #endregion

            // Update password.
            userProfile.password = SaltContainer.GetHashedPassword(password.PropertyValueString, Config.Active.Salt);

            // Update stored profile.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Update on SQL server.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTableAsync(
                    OperatingType, 
                    Session.Current.TerminationTokenSource.Token, 
                    userProfile);
            }
            else
            {
                // Ipdate in local storage.
                API.LocalUsers.SetProfile(userProfile);
            }

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
                asyncDataOperator = null;
            }
            
            // Log sucess if not failed.
            if(!dataOperationFailed)
            { 
                // Inform about success
                UniformServer.BaseServer.SendAnswerViaPP(
                    "success",
                    query);
            }

            #region SQL server callbacks
            // Looking for user on SQL server if connected.
            void ErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!userProfile.Equals(sender))
                {
                    return;
                }

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, query);
                dataOperationFailed = true;
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
            // USER prop.
            if(!query.QueryParamExist("user")) return false;

            // NEW prop.
            if (!query.QueryParamExist("update")) return false;

            // PASSWORD prop.
            if (!query.QueryParamExist("password")) return false;

            // OLD PASSWORD prop.
            if (!query.QueryParamExist("oldPassword")) return false;

            return true;
        }
    }
}
