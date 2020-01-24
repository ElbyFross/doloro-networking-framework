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
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Set new password for user.
    /// Require admin or certen user rights.
    /// </summary>
    public class USER_UPDATE_PASSWORD : IQueryHandler , IBaseTypeChangable
    {
        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public USER_UPDATE_PASSWORD()
        {
            OperatingType = TypeReplacer.GetValidType(typeof(User));
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
                out User userProfile,
                out Data.Temporal.TokenInfo tokenInfo))
            {
                // Drop if invalid. 
                return;
            }

            #region Validate old password.
            if (tokenInfo.userId == 0 || tokenInfo.userId == userProfile.id)
            {
                try
                {
                    // Comapre password with stored.
                    if (!userProfile.IsOpenPasswordCorrect(oldPassword.PropertyValueString))
                    {
                        // Inform that password is incorrect.
                        UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", query);
                        return;
                    }
                }
                catch
                {
                    // Inform that password is incorrect.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Confirm old password", query);
                    return;
                }
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
            //if (!query.QueryParamExist("oldPassword")) return false;

            return true;
        }
    }
}
