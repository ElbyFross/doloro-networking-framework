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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthorityController.Data;
using PipesProvider.Networking.Routing;

namespace AuthorityController
{
    /// <summary>
    /// Object that control data relative to current authority session.
    /// </summary>
    [System.Serializable]
    public class Session
    {
        #region Events
        /// <summary>
        /// Event that would called when provider would need to informate related servers by new data.
        /// </summary>
        public static event System.Action<string> InformateRelatedServers;
        #endregion

        #region Public properties and fields
        /// <summary>
        /// Last created session.
        /// </summary>
        public static Session Current
        {
            get
            {
                if(last == null)
                {
                    last = new Session();
                }
                return last;
            }

            protected set { last = value; }
        }
        #endregion

        #region Constructors
        public Session()
        {
            // Set this session as active.
            Current = this;
        }
        #endregion

        #region Private fields
        /// Object that contain current session.
        private static Session last;

        /// <summary>
        /// Table that contains rights provided to token.
        ///
        /// Key - string token
        /// Value - TokenInfo
        /// </summary>
        private readonly Hashtable tokensToInfo = new Hashtable();
        #endregion

        #region Public methods
        /// <summary>
        /// Create token registration binded for user profile.
        /// Not profided fields would filled like anonymous.
        /// Time stamp will contain the time of method call.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool AsignTokenToUser(User user, string token)
        {
            return AsignTokenToUser(
                user,
                token,
                "anonymous",
                "anonymous",
                DateTime.Now.ToBinary().ToString());
        }

        /// <summary>
        /// Create token registration binded for user profile.
        /// </summary>
        /// <param name="user">User profile that contain core data.</param>
        /// <param name="token">Token provided to that user.</param>
        /// <param name="mac">Mac adress of user machine.</param>
        /// <param name="os">OS of user.</param>
        /// <param name="stamp">Time stamp that show when the session was started.</param>
        /// <returns></returns>
        public bool AsignTokenToUser(User user, string token, string mac, string os, string stamp)
        {
            // Update rights if already exist.
            if (tokensToInfo[token] is TokenInfo)
            {
                // Inform that token alredy asigned.
                return false;
            }

            TokenInfo info =
                new TokenInfo()
                {
                    token = token,
                    userId = user.id,
                    machineMac = mac,
                    operationSystem = os,
                };

            if (!long.TryParse(stamp, out info.allocationTime))
            {
                // Invelid stamp format.
                return false;
            }

            // Create token registration for this user id.
            tokensToInfo.Add(
                token,
                info);

            // Inform about success.
            return true;
        }

        /// <summary>
        /// Set rights' codes array as relative to token.
        ///
        /// In case if token infor not registred then will create anonimouse info with applied rights.
        /// Applicable to purposes of servers that depends to session provider one,
        /// but not require entire token information, cause not manage it.
        /// </summary>
        /// <param name="token">Session token.</param>
        /// <param name="rights">Array og rights' codes.</param>
        public void SetTokenRights(string token, params string[] rights)
        {
            // Update rights if already exist.
            if (tokensToInfo.ContainsKey(token))
            {
                // Loading token info.
                TokenInfo info = (TokenInfo)tokensToInfo[token];

                // If not anonymous user.
                if (API.Users.TryToFindUser(info.userId, out User user))
                {
                    // Update every token.
                    foreach(string additiveTokens in user.tokens)
                    {
                        // Loading toking info.
                        TokenInfo additiveInfo = (TokenInfo)tokensToInfo[additiveTokens];

                        // Update rights.
                        additiveInfo.rights = rights;

                        // Send info to relative servers.
                        ShareTokenRights(additiveTokens, rights);
                    }
                }
                // if user not detected.
                else
                {
                    // Update rights.
                    info.rights = rights;

                    // Send info to relative servers.
                    ShareTokenRights(token, rights);
                }
            }
            // If user was not loaded.
            else
            {
                // Create anonymous container.
                TokenInfo info = TokenInfo.Anonymous;

                // Apply fields.
                info.token = token;
                info.rights = rights;

                // Set as new.
                tokensToInfo.Add(token, info);

                // Send info to relative servers.
                ShareTokenRights(token, rights);
            }
        }

        /// <summary>
        /// Trying to load rights registred for token.
        /// </summary>
        /// <param name="token">Session token.</param>
        /// <param name="rights">Array of rights' codes relative to token.</param>
        /// <returns></returns>
        public bool TryGetTokenRights(string token, out string[] rights)
        {
            // Try to get regustred rights.
            if (tokensToInfo[token] is Data.TokenInfo rightsBufer)
            {
                rights = rightsBufer.rights;
                return true;
            }

            // Inform about fail.
            rights = null;
            return false;
        }

        /// <summary>
        ///Removing of token from table and inform relative servers about that.
        /// </summary>
        /// <param name="token"></param>
        public bool SetExpired(string token)
        {
            if (RemoveToken(token))
            {
                // Compose query that will shared to related servers to update them local data.
                string informQuery = string.Format("set{0}token={1}{0}expired",
                    UniformQueries.API.SPLITTING_SYMBOL,
                    token);

                // Send query to infrom related servers about event.
                InformateRelatedServers?.Invoke(informQuery);

                // Confirm token removing.
                return true;
            }

            // Inform that token not found.
            return false;
        }

        /// <summary>
        /// Try to find registred token info.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool TryGetTokenInfo(string token, out TokenInfo info)
        {
            if(tokensToInfo[token] is TokenInfo bufer)
            {
                info = bufer;
                return true;
            }

            info = TokenInfo.Anonymous;
            return false;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Removing token from table.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Is removed successful?</returns>
        private bool RemoveToken(string token)
        {
            try
            {
                // If user not anonymous.
                if (tokensToInfo[token] is TokenInfo info)
                {
                    // Try to get user by id.
                    // Would found if server not anonymous and has registred data about user.
                    if(API.Users.TryToFindUser(info.userId, out User user))
                    {
                        // Remove tokens from registred list.
                        user.tokens.Remove(token);
                    }

                    // Unregister token from table.
                    tokensToInfo.Remove(token);

                    // Conclude success of operation.
                    return true;
                }

                // Conclude that token registration not found.
                Console.WriteLine("TOKEN REMOVING: Failed\nToken not registred.");
                return false;
            }
            catch (Exception ex)
            {
                // Log about error.
                Console.WriteLine("TOKEN REMOVING ERROR:\n{0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sending new rights of token to related servers.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rights"></param>
        private void ShareTokenRights(string token, string[] rights)
        {
            // Composing query that will shared to related servers for update them local data.
            string informQuery = string.Format("set{0}token={1}{0}rights=",
                UniformQueries.API.SPLITTING_SYMBOL,
                token);

            // Adding rights' codes.
            foreach (string rightsCode in rights)
            {
                // Add every code splited by '+'.
                informQuery += "+" + rightsCode;
            }

            // Sending query to inform related servers about event.
            InformateRelatedServers?.Invoke(informQuery);
        }
        #endregion
    }
}
