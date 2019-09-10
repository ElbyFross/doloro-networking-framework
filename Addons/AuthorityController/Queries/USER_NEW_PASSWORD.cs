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
    public class USER_NEW_PASSWORD : IQueryHandler
    {
        public virtual string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER NEW PASSWORD\n" +
                            "\tDESCRIPTION: Request new password for user." +
                            "\n\tToken confirm rights to change it.\n" +
                            "\n\tOld password required to avoid access from public places.\n" +
                            "\tQUERY FORMAT: user=..." + UniformQueries.API.SPLITTING_SYMBOL + 
                            "new" + UniformQueries.API.SPLITTING_SYMBOL +
                            "password=..." + UniformQueries.API.SPLITTING_SYMBOL + 
                            "oldPassword=..." + UniformQueries.API.SPLITTING_SYMBOL +
                            "token=..." + "\n";
            }
        }

        public virtual void Execute(QueryPart[] queryParts)
        {
            bool dataOperationFailed = false;
            string error = null;

            #region Get params.
            UniformQueries.API.TryGetParamValue("user",         out QueryPart user, queryParts);
            UniformQueries.API.TryGetParamValue("password",     out QueryPart password, queryParts);
            UniformQueries.API.TryGetParamValue("oldPassword",  out QueryPart oldPassword, queryParts);
            UniformQueries.API.TryGetParamValue("token",        out QueryPart token, queryParts);
            #endregion

            User userProfile = (User)Activator.CreateInstance(User.GlobalType);
            userProfile.login = user.propertyValue;

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
                    User.GlobalType,
                    Session.Current.TerminationToken,
                    userProfile,
                    new string[0],
                    new string[] { "login" });
                #endregion
            }
            // Looking for user in local storage.
            else
            {
                #region Local storage
                if (!API.LocalUsers.TryToFindUserUniform(user.propertyValue, out userProfile, out error))
                {
                    // Inform about error.
                    UniformServer.BaseServer.SendAnswerViaPP(error, queryParts);
                    return;
                }
                #endregion
            }
            #endregion

            // If async operation started.
            if (asyncDataOperator != null)
            {
                // Wait until finishing.
                while (!asyncDataOperator.IsCompleted || !asyncDataOperator.IsCanceled)
                {
                    Thread.Sleep(5);
                }

                // Unsubscribe from errors listening.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= ErrorListener;
                asyncDataOperator = null;
            }

            #region Check base requester rights
            if (!API.Tokens.IsHasEnoughRigths(
                token.propertyValue,
                out string[] requesterRights,
                out error,
                Config.Active.QUERY_UserNewPassword_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, queryParts);
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
                if (token.propertyValue == userToken)
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
                if(!API.Collections.TyGetPropertyValue("rank", out string userRank, userProfile.rights))
                {
                    // Inform that rights not enough.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: User rank not defined", queryParts);
                    return;
                }

                // Check token rights.
                if (!API.Collections.IsHasEnoughRigths(requesterRights,
                    // Request hiegher rank then user and at least moderator level.
                    ">rank=" + userRank, ">rank=2"))
                {
                    // Inform that rank not defined.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", queryParts);
                    return;
                }
            }
            #endregion

            #region Validate password.
            // Comapre password with stored.
            if (!userProfile.IsOpenPasswordCorrect(oldPassword.propertyValue))
            {
                // Inform that password is incorrect.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 412: Incorrect password", queryParts);
                return;
            }
            #endregion

            #region Validate new password
            if(!API.Validation.PasswordFormat(password.propertyValue, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    queryParts);
                return;
            }
            #endregion

            // Update password.
            userProfile.password = SaltContainer.GetHashedPassword(password.propertyValue, Config.Active.Salt);

            // Update stored profile.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                // Update on SQL server.
                asyncDataOperator = UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTableAsync(
                    User.GlobalType, 
                    Session.Current.TerminationToken, 
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
                while (!asyncDataOperator.IsCompleted || !asyncDataOperator.IsCanceled)
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
                    queryParts);
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
                UniformServer.BaseServer.SendAnswerViaPP("ERROR SQL SERVER: " + message, queryParts);
                dataOperationFailed = true;
            }
            #endregion 
        }

        public virtual bool IsTarget(QueryPart[] queryParts)
        {
            // USER prop.
            if(!UniformQueries.API.QueryParamExist("user", queryParts))
                return false;

            // NEW prop.
            if (!UniformQueries.API.QueryParamExist("new", queryParts))
                return false;

            // PASSWORD prop.
            if (!UniformQueries.API.QueryParamExist("password", queryParts))
                return false;

            // OLD PASSWORD prop.
            if (!UniformQueries.API.QueryParamExist("oldPassword", queryParts))
                return false;

            return true;
        }
    }
}
