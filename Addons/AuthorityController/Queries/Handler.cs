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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniformQueries;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Class that allow to simplify base operations with queries specified to AuthorityController.
    /// </summary>
    public static class Handler
    {
        /// <summary>
        /// Checks does the token has enought rights to perform a query.
        /// 
        /// Demands `user` and `token` params at the query.
        /// 
        /// Attention:
        /// Requerster must has higher rank.
        /// Requester must be at least moderator to affect other users. (rank=2+).
        /// 
        /// -OR-
        /// 
        /// Requester must be the same user as defined at the `user` param.
        /// </summary>
        /// <param name="entryQuery">
        /// Query received by server. 
        /// Must contain the token and user property.
        /// </param>
        /// <param name="userType">
        /// Type of user that will be used as table descriptor during sql queries.
        /// </param>
        /// <param name="requiredRights">
        /// Rights required to operation.
        /// </param>
        /// <param name="error">
        /// Error message in case if received.
        /// </param>
        /// <param name="targetUser">
        /// Profile of target user in case if detected by the `user` params's value.
        /// </param>
        /// <param name="tokenInfo"
        /// >A found token info relative the `token` param's value.
        /// </param>
        /// <returns>
        /// Is token authorized to operation. 
        /// In case of fail server auto send answer with error to the client by using the entryQuery.
        /// </returns>
        public static bool ValidateUserRights(
            Type userType,
            Query entryQuery,
            string[] requiredRights,
            out string error,
            out User targetUser, 
            out Data.Temporal.TokenInfo tokenInfo)
        {
            // Set defaults.
            bool dataOperationFailed = false;
            targetUser = null;
            tokenInfo = null;
            error = null;

            #region Get params.
            entryQuery.TryGetParamValue("user", out QueryPart user);
            entryQuery.TryGetParamValue("token", out QueryPart token);
            #endregion

            #region Looking for a profile of the user from the `user` praram.
            User userProfile = (User)Activator.CreateInstance(userType);
            userProfile.login = user.PropertyValueString;

            Task asyncDataOperator = null;
            // Get data from SQL server if connected.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                #region SQL server                
                // Subscribe on errors.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += ErrorListener;

                // Request data.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObjectAsync(
                    userType,
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
                    UniformServer.BaseServer.SendAnswerViaPP(error, entryQuery);
                    return false;
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

            // Drop if operation with data failed.
            if (dataOperationFailed)
            {
                error = "Data operator failed.";
                return false;
            }

            #region Check base requester rights
            if (!API.Tokens.IsHasEnoughRigths(
                token.PropertyValueString,
                out tokenInfo,
                out error,
                requiredRights))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, entryQuery);
                return false;
            }
            #endregion

            #region Check rank permition
            // If not the self update request, then check rights to moderate.
            if (tokenInfo.userId != userProfile.id)
            {
                // Get target User's rank.
                if (!API.Collections.TryGetPropertyValue("rank", out string userRank, userProfile.rights))
                {
                    // Inform that rights not enough.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: User rank not defined", entryQuery);
                    return false;
                }

                // Check token rights.
                if (!API.Collections.IsHasEnoughRigths(tokenInfo.rights,
                    ">rank=" + userRank, ">rank=2"))
                {
                    // Inform that rank not defined.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", entryQuery);
                    return false;
                }
            }
            #endregion

            // Conclude success.
            targetUser = userProfile;
            return true;

            #region SQL server callbacks
            // Looking for user on SQL server if connected.
            void ErrorListener(object sender, string message)
            {
                // Drop if not target user.
                if (!userProfile.Equals(sender)) return;

                // Unsubscribe.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;

                // Inform that user not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, entryQuery);
                dataOperationFailed = true;
            }
            #endregion 
        }

    }
}
