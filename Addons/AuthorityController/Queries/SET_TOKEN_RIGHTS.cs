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
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Application;
using AuthorityController.Data.Personal;
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Change rights list to provided token.
    /// Require admin rights.
    /// </summary>
    public class SET_TOKEN_RIGHTS : IQueryHandler, IBaseTypeChangable
    {        
        /// <summary>
        ///  Type that will be used in operations.
        /// </summary>
        public Type OperatingType { get; set; }

        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public SET_TOKEN_RIGHTS()
        {
            OperatingType = TypeReplacer.GetValidType(typeof(User));
        }

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
                    return "SET TARGETTOKEN RIGHTS\n" +
                            "\tDESCRIPTION: Provide guest key for passing of base authority level\n" +
                            "\tQUERY FORMAT: TOKEN=requesterToken & " +
                            "SET & targetToken=..." +
                            " & RIGHTS=rightCode1+rightCode2+...\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public void Execute(object sender, Query query)
        {
            #region Get fields from query
            // Get params.
            query.TryGetParamValue("token", out QueryPart token);
            query.TryGetParamValue("targetToken", out QueryPart targetToken);
            query.TryGetParamValue("rights", out QueryPart rights);
            #endregion

            #region Check requester rights
            if (!API.Tokens.IsHasEnoughRigths(
                token.PropertyValueString,
                out Data.Temporal.TokenInfo tokenInfo,
                out string error,
                Config.Active.QUERY_SetTokenRights_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, query);
                return;
            }
            #endregion

            #region Get target token rights
            if (!Session.Current.TryGetTokenInfo(targetToken.PropertyValueString, out Data.Temporal.TokenInfo targetTokenInfo))
            {
                // If also not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: Target token not registred", query);
                return;
            }
            #endregion

            #region Compare ranks
            // Get target User's rank.
            if (!API.Collections.TryGetPropertyValue("rank", out string userRank, targetTokenInfo.rights))
            {
                // Mean that user has a guest rank.
                userRank = "0";
            }

            // Check is the target user has the less rank then requester.
            if (!API.Collections.IsHasEnoughRigths(tokenInfo.rights, ">rank=" + userRank))
            {
                // Inform that target user has the same or heigher rank then requester.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", query);
                return;
            }
            #endregion

            // Apply new rights
            string[] rightsArray = rights.PropertyValueString.Split('+');
            Session.Current.SetTokenRights(targetToken.PropertyValueString, rightsArray);

            //if (targetTokenInfo.userId > 0)
            //{
            //    if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            //    {

            //        #region SQL server
            //        // Start new task for providing possibility to terminate all operation by
            //        // session's cancelation token. 
            //        // In other case after termination on any internal task other will started.
            //        Task.Run(async delegate ()
            //        {
            //            // Instiniating new profile.
            //            var targetUserProfile = new User
            //            {
            //                // Define an id.
            //                id = targetTokenInfo.userId,

            //                // Set new rights.
            //                rights = rightsArray,

            //                // Dropping data.
            //                firstName = null,
            //                lastName = null,
            //                bans = null
            //            };

            //            // A handler that will be called in case of SQL error.
            //            void SQLErrorhandler(object errorSender, string _)
            //            {
            //                // Drop if not target user.
            //                if (!targetUserProfile.Equals(errorSender))
            //                {
            //                    return;
            //                }

            //                // Unsubscribe.
            //                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorhandler;
            //            }

            //            // Subscribing on the SQL errors.
            //            UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += SQLErrorhandler;

            //            try
            //            {
            //                // Sending data to the 
            //                await UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTableAsync(
            //                    OperatingType,
            //                    Session.Current.TerminationTokenSource.Token,
            //                    targetUserProfile);
            //            }
            //            catch (Exception ex)
            //            {
            //                UniformServer.BaseServer.SendAnswerViaPP("SQL ERROR: " + ex.Message, query);
            //                return;
            //            }
            //            finally
            //            {
            //                // Unsubscribe the errors listener.
            //                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorhandler;
            //            }

            //            // Inform about success.
            //            UniformServer.BaseServer.SendAnswerViaPP("success", query);
            //        },
            //        Session.Current.TerminationTokenSource.Token);
            //        #endregion
            //    }
            //    else UniformServer.BaseServer.SendAnswerViaPP("success", query);
            //}
            //else UniformServer.BaseServer.SendAnswerViaPP("success", query);

            UniformServer.BaseServer.SendAnswerViaPP("success", query);
        }

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="query">Recived query.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(Query query)
        {
            // Request set property.
            
            if (!query.QueryParamExist("set"))
                return false;

            // Token that will be a target in case if requester has enough rights to do this.
            if (!query.QueryParamExist("targetToken"))
                return false;

            // List of rights' keys.
            if (!query.QueryParamExist("rights"))
                return false;

            return true;
        }
    }
}
