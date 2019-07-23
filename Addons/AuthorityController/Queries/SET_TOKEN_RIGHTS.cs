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

namespace AuthorityController.Queries
{
    /// <summary>
    /// Change rights list to provided token.
    /// Require admin rights.
    /// </summary>
    public class SET_TOKEN_RIGHTS : IQueryHandler
    {
        public string Description(string cultureKey)
        {
            throw new NotImplementedException();
        }

        public void Execute(QueryPart[] queryParts)
        {
            #region Get fields from query
            // Get params.
            UniformQueries.API.TryGetParamValue("token", out QueryPart token, queryParts);
            UniformQueries.API.TryGetParamValue("targetToken", out QueryPart targetToken, queryParts);
            UniformQueries.API.TryGetParamValue("rights", out QueryPart rights, queryParts);
            #endregion

            #region Check requester rights
            if (!API.Tokens.IsHasEnoughRigths(
                token.propertyValue,
                out string[] requesterRights,
                out string error,
                Config.Active.QUERY_SetTokenRights_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, queryParts);
                return;
            }
            #endregion

            #region Get target token rights
            if (!Session.Current.TryGetTokenRights(targetToken.propertyValue, out string[] targetTokenRights))
            {
                // If also not found.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: User not found", queryParts);
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
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Unauthorized", queryParts);
                return;
            }
            #endregion

            // Apply new rights
            string[] rightsArray = rights.propertyValue.Split('+');
            Session.Current.SetTokenRights(targetToken.propertyValue, rightsArray);
            
            // Inform about success.
            UniformServer.BaseServer.SendAnswerViaPP("Success", queryParts);
        }

        public bool IsTarget(QueryPart[] queryParts)
        { 
            // Request set property.
            if (!UniformQueries.API.QueryParamExist("set", queryParts))
                return false;

            // Token that will be a target in case if requester has enough rights to do this.
            if (!UniformQueries.API.QueryParamExist("targetToken", queryParts))
                return false;

            // List of rights' keys.
            if (!UniformQueries.API.QueryParamExist("rights", queryParts))
                return false;

            return true;
        }
    }
}
