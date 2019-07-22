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
using AuthorityController.Data.Temporal;
using AuthorityController.Data.Personal;
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
        //public static event System.Action<string> InformateRelatedServers;
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

        /// <summary>
        /// Routing table that contain instructions to access reletive servers
        /// that need to be informed about token events.
        /// 
        /// Before sharing query still will check is the query stituable for that routing instruction.
        /// If you no need any filtring then just leave query patterns empty.
        /// </summary>
        public static RoutingTable RelatedServers { get; set; }
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
        private readonly Hashtable tokensInfo = new Hashtable();
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
            if (tokensInfo[token] is TokenInfo)
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
            tokensInfo.Add(
                token,
                info);

            // Inform about success.
            return true;
        }

        /// <summary>
        /// TODO Implement SetTokenRightsAuto
        /// Set rights' codes array as relative to token.
        /// 
        /// Would automaticly share information to related servers if it posssible.
        /// </summary>
        /// <param name="targetToken"></param>
        /// <param name="rights"></param>
        public void SetTokenRightsAuto(string targetToken, params string[] rights)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set rights' codes array as relative to token.
        /// Update rights only on local authority controller.
        /// </summary>
        /// <param name="targetToken">Session token.</param>
        /// <param name="rights">Array og rights' codes.</param>
        public void SetTokenRightsLocal(string targetToken, params string[] rights)
        {
            SetTokenRightsPublic(null, targetToken, rights);
        }

        /// <summary>
        /// Set rights' codes array as relative to token.
        ///
        /// Make and share SET TOKEN RIGHTS query to every related server.
        /// Share only anonymous format of information. Personal data would be droped.
        /// </summary>
        /// <param name="requesterToken">"Token that would be shared to other servers in query to confirm rights.
        /// Attention: server need to be logined as autorized user on every related server."</param>
        /// <param name="targetToken">Token that need to change rights.</param>
        /// <param name="rights">List of new rights allowed to target toekn.</param>
        public void SetTokenRightsPublic(string requesterToken, string targetToken, params string[] rights)
        {
            // Update rights if already exist.
            if (tokensInfo.ContainsKey(targetToken))
            {
                // Loading token info.
                TokenInfo info = (TokenInfo)tokensInfo[targetToken];

                // If not anonymous user.
                if (API.Users.TryToFindUser(info.userId, out User user))
                {
                    // Update every token.
                    foreach(string subTargetToken in user.tokens)
                    {
                        // Loading toking info.
                        TokenInfo additiveInfo = (TokenInfo)tokensInfo[subTargetToken];

                        // Update rights.
                        additiveInfo.rights = rights;

                        // Send info to relative servers.
                        ShareTokenRights_MessageProcessor(requesterToken, subTargetToken, rights);
                    }
                }
                // if user not detected.
                else
                {
                    // Update rights.
                    info.rights = rights;

                    // Send info to relative servers.
                    ShareTokenRights_MessageProcessor(requesterToken, targetToken, rights);
                }
            }
            // If user was not loaded.
            else
            {
                // Create anonymous container.
                TokenInfo info = TokenInfo.Anonymous;

                // Apply fields.
                info.token = targetToken;
                info.rights = rights;

                // Set as new.
                tokensInfo.Add(targetToken, info);

                // Send info to relative servers.
                ShareTokenRights_MessageProcessor(requesterToken, targetToken, rights);
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
            if (tokensInfo[token] is TokenInfo rightsBufer)
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
                SendMessageToRelatedServers(informQuery);

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
            if(tokensInfo[token] is TokenInfo bufer)
            {
                info = bufer;
                return true;
            }

            info = TokenInfo.Anonymous;
            return false;
        }

        /// <summary>
        /// Call event that request sanding mssage to related servers.
        /// </summary>
        /// <param name="message">Message that would be shared.</param>
        public void SendMessageToRelatedServers(string message)
        {
            // Inform relative servers.
            if (RelatedServers != null)
            {
                // Check every instruction.
                for (int i = 0; i < RelatedServers.intructions.Count; i++)
                {
                    // Get instruction.
                    Instruction instruction = RelatedServers.intructions[i];

                    // Does instruction situable to query.
                    if (!instruction.IsRoutingTarget(message))
                    {
                        // Skip if not.
                        continue;
                    }

                    // Open transmission line to server.
                    UniformClient.BaseClient.OpenOutTransmissionLineViaPP(instruction.routingIP, instruction.pipeName).
                        EnqueueQuery(message).                  // Add query to queue.
                        SetInstructionAsKey(ref instruction).   // Apply encryption if requested.
                        TryLogonAs(instruction.logonConfig);    // Profide logon data to access remote machine.
                }
            }
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
                if (tokensInfo[token] is TokenInfo info)
                {
                    // Try to get user by id.
                    // Would found if server not anonymous and has registred data about user.
                    if(API.Users.TryToFindUser(info.userId, out User user))
                    {
                        // Remove tokens from registred list.
                        user.tokens.Remove(token);
                    }

                    // Unregister token from table.
                    tokensInfo.Remove(token);

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
        /// <param name="targetToken"></param>
        /// <param name="rights"></param>
        private void ShareTokenRights_MessageProcessor(string requesterToken, string targetToken, string[] rights)
        {
            // Skip undefined requsester queryes.
            // Suck kind of queries is equal.
            if (requesterToken == null)
            {
                return;
            }

            // Composing query that will shared to related servers for update them local data.
            string informQuery = string.Format("set{0}token={2}{0}targetToken={1}{0}rights=",
                UniformQueries.API.SPLITTING_SYMBOL,
                targetToken,
                requesterToken);

            // Adding rights' codes.
            foreach (string rightsCode in rights)
            {
                // Add every code splited by '+'.
                informQuery += "+" + rightsCode;
            }

            // Sending query to inform related servers about event.
            SendMessageToRelatedServers(informQuery);
        }
        #endregion
    }
}
