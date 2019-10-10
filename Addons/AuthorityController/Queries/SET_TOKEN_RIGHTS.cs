﻿//Copyright 2019 Volodymyr Podshyvalov
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

namespace AuthorityController.Queries
{
    /// <summary>
    /// Change rights list to provided token.
    /// Require admin rights.
    /// </summary>
    public class SET_TOKEN_RIGHTS : IQueryHandler
    {
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
                out string[] requesterRights,
                out string error,
                Config.Active.QUERY_SetTokenRights_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, query);
                return;
            }
            #endregion

            #region Get target token rights
            if (!Session.Current.TryGetTokenRights(targetToken.PropertyValueString, out string[] targetTokenRights))
            {
                // If also not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: User not found", query);
                return;
            }
            #endregion

            #region Compare ranks
            // Get target User's rank.
            if (!API.Collections.TyGetPropertyValue("rank", out string userRank, targetTokenRights))
            {
                // Mean that user has a guest rank.
                userRank = "0";
            }

            // Check is the target user has the less rank then requester.
            if (!API.Collections.IsHasEnoughRigths(requesterRights, ">rank=" + userRank))
            {
                // Inform that target user has the same or heigher rank then requester.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", query);
                return;
            }
            #endregion

            // Apply new rights
            string[] rightsArray = rights.PropertyValueString.Split('+');
            Session.Current.SetTokenRights(targetToken.PropertyValueString, rightsArray);
            
            // Inform about success.
            UniformServer.BaseServer.SendAnswerViaPP("Success", query);
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
