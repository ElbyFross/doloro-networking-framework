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
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using UniformQueries;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Set temporaly or permanent ban for user.
    /// </summary>
    public class USER_BAN : IQueryHandler
    {
        public string Description(string cultureKey)
        {
            throw new NotImplementedException();
        }

        public void Execute(QueryPart[] queryParts)
        {
            string error;

            #region Get params
            // Get requestor token.
            UniformQueries.API.TryGetParamValue("token", out QueryPart token, queryParts);

            // Get target user id or login.
            UniformQueries.API.TryGetParamValue("user", out QueryPart user, queryParts);

            // XML serialized BanInformation. If empty then will shared permanent ban.
            UniformQueries.API.TryGetParamValue("ban", out QueryPart ban, queryParts);
            #endregion

            #region Check token rights.
            if (!API.Tokens.IsHasEnoughRigths(
                token.propertyValue,
                out string[] requesterRights,
                out error,
                Data.Config.Active.QUERY_UserBan_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, queryParts);
                return;
            }
            #endregion

            #region Detect target user
            // Find user for ban.
            if (!API.Users.TryToFindUserUniform(user.propertyValue, out Data.User userProfile, out error))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, queryParts);
                return;
            }
            #endregion

            #region Compare ranks
            // Get target User's rank.
            if (!API.Collections.TyGetPropertyValue("rank", out string userRank, userProfile.rights))
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

            #region Apply ban
            Data.BanInformation banInfo;
            if (!string.IsNullOrEmpty(ban.propertyValue))
            {
                // Get ban information.
                if (!Data.Handler.TryXMLDeserizlize<Data.BanInformation>
                    (ban.propertyValue, out banInfo))
                {

                    // If also not found.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: Ban information corrupted.", queryParts);
                    return;
                }
            }
            else
            {
                // Set auto configurated permanent ban if detail not described.
                banInfo = Data.BanInformation.Permanent;
            }

            // Add ban to user.
            userProfile.bans.Add(banInfo);

            // Update stored profile.
            // in other case ban will losed after session finishing.
            API.Users.SetProfile(userProfile);
            
            // Inform about success.
            UniformServer.BaseServer.SendAnswerViaPP("Success", queryParts);
            #endregion
        }

        public bool IsTarget(QueryPart[] queryParts)
        { 
            // USER prop.
            if (!UniformQueries.API.QueryParamExist("user", queryParts))
                return false;

            // BAN prop.
            if (!UniformQueries.API.QueryParamExist("ban", queryParts))
                return false;

            return true;
        }
    }
}
