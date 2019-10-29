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
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Set temporaly or permanent ban for user.
    /// </summary>
    public class USER_BAN : IQueryHandler, UniformDataOperator.Modifiers.IBaseTypeChangable
    {
        /// <summary>
        /// Base constructor.
        /// Defining operating type.
        /// </summary>
        public USER_BAN()
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
                    return "TOKEN BAN USER\n" +
                            "\tDESCRIPTION: Ban user by login or id.\n" +
                            "\tQUERY FORMAT: TOKEN=requesterToken&" +
                            "BAN=BanInfoXML&USER=userID\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public virtual void Execute(object sender, Query query)
        {
            #region Get params
            // XML serialized BanInformation. If empty then will shared permanent ban.
            query.TryGetParamValue("ban", out QueryPart ban);
            #endregion

            // Validate user rights to prevent not restricted acess passing.
            if (!Handler.ValidateUserRights(
                OperatingType,
                query,
                Config.Active.QUERY_UserBan_RIGHTS,
                out _,
                out User userProfile))
            {
                // Drop if invalid. 
                return;
            }

            #region Apply ban
            BanInformation banInfo;
            if (!string.IsNullOrEmpty(ban.PropertyValueString))
            {
                // Get ban information.
                if (UniformDataOperator.Binary.BinaryHandler.FromByteArray(ban.propertyValue) 
                    is BanInformation banInfoBufer)
                {
                    banInfo = banInfoBufer;
                }
                else
                {
                    // If also not found.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: Ban information corrupted.", query);
                    return;
                }
            }
            else
            {
                // Set auto configurated permanent ban if detail not described.
                banInfo = BanInformation.Permanent;
            }
            
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active == null)
            {
                // Add ban to user.
                userProfile.bans.Add(banInfo);

                // Update stored profile.
                // in other case ban will losed after session finishing.
                API.LocalUsers.SetProfile(userProfile);
            }
            else
            {
                // XML serialized BanInformation. If empty then will shared permanent ban.
                query.TryGetParamValue("token", out QueryPart token);

                banInfo.userId = userProfile.id;
                // Try to find the requester id.
                if(Session.Current.TryGetTokenInfo(token.PropertyValueString, out Data.Temporal.TokenInfo tokenInfo))
                {
                    banInfo.bannedByUserId = tokenInfo.userId;
                }

                string error;
                if (!UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToTable(
                    typeof(BanInformation),
                    banInfo,
                    out error))
                {
                    UniformServer.BaseServer.SendAnswerViaPP(error, query);
                    return;
                }
            }

            // Inform about success.
            UniformServer.BaseServer.SendAnswerViaPP("Success", query);
            #endregion
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public virtual void Execute2(object sender, Query query)
        {

            #region Get params
            // Get requestor token.
            query.TryGetParamValue("token", out QueryPart token);

            // Get target user id or login.
            query.TryGetParamValue("user", out QueryPart user);

            // XML serialized BanInformation. If empty then will shared permanent ban.
            query.TryGetParamValue("ban", out QueryPart ban);
            #endregion

            #region Check token rights.
            if (!API.Tokens.IsHasEnoughRigths(
                token.PropertyValueString,
                out Data.Temporal.TokenInfo tokenInfo,
                out string error,
                Config.Active.QUERY_UserBan_RIGHTS))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, query);
                return;
            }
            #endregion

            #region Detect target user
            // Find user for ban.
            if (!API.LocalUsers.TryToFindUserUniform(user.PropertyValueString, out User userProfile, out error))
            {
                // Inform about error.
                UniformServer.BaseServer.SendAnswerViaPP(error, query);
                return;
            }
            #endregion

            #region Compare ranks
            // Get target User's rank.
            if (!API.Collections.TryGetPropertyValue("rank", out string userRank, userProfile.rights))
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

            #region Apply ban
            BanInformation banInfo;
            if (!string.IsNullOrEmpty(ban.PropertyValueString))
            {
                if (UniformDataOperator.Binary.BinaryHandler.FromByteArray(ban.propertyValue)
                       is BanInformation banInfoBufer)
                {
                    banInfo = banInfoBufer;
                }
                else
                {

                    // If also not found.
                    UniformServer.BaseServer.SendAnswerViaPP("ERROR 404: Ban information corrupted.", query);
                    return;
                }
            }
            else
            {
                // Set auto configurated permanent ban if detail not described.
                banInfo = BanInformation.Permanent;
            }

            // Add ban to user.
            userProfile.bans.Add(banInfo);

            // Update stored profile.
            // in other case ban will losed after session finishing.
            API.LocalUsers.SetProfile(userProfile);
            
            // Inform about success.
            UniformServer.BaseServer.SendAnswerViaPP("Success", query);
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
            if (!query.QueryParamExist("user"))
                return false;

            // BAN prop.
            if (!query.QueryParamExist("ban"))
                return false;

            return true;
        }
    }
}
