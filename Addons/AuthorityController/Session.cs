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
using System.Threading;
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
        #region Public properties and fields
        /// <summary>
        /// Current active session.
        /// </summary>
        public static Session Current
        {
            get
            {
                if(_current == null)
                {
                    _current = new Session();
                }
                return _current;
            }

            set { _current = value; }
        }

        /// <summary>
        /// Routing table that contain instructions to access reletive servers
        /// that need to be informed about token events.
        /// 
        /// Before sharing query still will check is the query stituable for that routing instruction.
        /// If you no need any filtring then just leave query patterns empty.
        /// </summary>
        public RoutingTable AuthorityFollowers { get; set; }

        /// <summary>
        /// Token that provide possibility to terminate all stated tasks in this session.
        /// </summary>
        public CancellationToken TerminationToken
        {
            get
            {
                if(_terminationToken == null)
                {
                    _terminationToken = new CancellationToken();
                }

                return _terminationToken;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Instiniate session object.
        /// Set it to Current property.
        /// </summary>
        public Session()
        {
            // Set this session as active.
            Current = this;

            // Initialize Termination token.
            _ = TerminationToken;
        }
        #endregion

        #region Private fields
        /// Object that contain current session.
        private static Session _current;

        private CancellationToken _terminationToken;

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
        /// Set rights' codes array as relative to token.
        ///
        /// Make and share SET TOKEN RIGHTS query to every related server.
        /// Share only anonymous format of information. Personal data would be droped.
        /// </summary>
        /// <param name="targetToken">Token that need to change rights.</param>
        /// <param name="rights">List of new rights allowed to target toekn.</param>
        public void SetTokenRights(string targetToken, params string[] rights)
        {
            // Update rights if already exist.
            if (tokensInfo.ContainsKey(targetToken))
            {
                // Loading token info.
                TokenInfo info = (TokenInfo)tokensInfo[targetToken];

                // If not anonymous user.
                if (API.LocalUsers.TryToFindUser(info.userId, out User user))
                {
                    // Update every token.
                    foreach(string subTargetToken in user.tokens)
                    {
                        // Loading toking info.
                        TokenInfo additiveInfo = (TokenInfo)tokensInfo[subTargetToken];

                        // Update rights.
                        additiveInfo.rights = rights;

                        // Send info to relative servers.
                        ShareTokenRights_MessageProcessor(subTargetToken, rights);
                    }
                }
                // if user not detected.
                else
                {
                    // Update rights.
                    info.rights = rights;

                    // Send info to relative servers.
                    ShareTokenRights_MessageProcessor(targetToken, rights);
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
                ShareTokenRights_MessageProcessor(targetToken, rights);
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
                InformAuthorityFollowers(informQuery);

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
        /// Sanding the message to all authority following servers descibed in 
        /// AuthorityFollowers table.
        /// </summary>
        /// <param name="message">Message that would be shared.</param>
        public void InformAuthorityFollowers(string message)
        {
            // Inform relative servers.
            if (AuthorityFollowers != null)
            {
                // Check every instruction.
                for (int i = 0; i < AuthorityFollowers.intructions.Count; i++)
                {
                    // Get instruction.
                    Instruction instruction = AuthorityFollowers.intructions[i];

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
                    if(API.LocalUsers.TryToFindUser(info.userId, out User user))
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
        private void ShareTokenRights_MessageProcessor(string targetToken, string[] rights)
        {
            // Drop invalid or unnecessary  routing.
            if (AuthorityFollowers == null ||
                AuthorityFollowers.intructions == null ||
                AuthorityFollowers.intructions.Count == 0)
            {
                return;
            }

            // Composing query that will shared to related servers for update them local data.
            string baseQuery = string.Format("set{0}targetToken={1}{0}rights=",
                UniformQueries.API.SPLITTING_SYMBOL,
                targetToken);

            // Adding rights' codes.
            foreach (string rightsCode in rights)
            {
                // Add every code splited by '+'.
                baseQuery += "+" + rightsCode;
            }
            
            // Send query to every following server.
            foreach (AuthorizedInstruction authInstr in AuthorityFollowers.intructions)
            {
                // Relogon if not logon or token exipred.
                if (string.IsNullOrEmpty(authInstr.AuthorizedToken) ||
                    UniformQueries.Tokens.IsExpired(authInstr.AuthorizedToken, authInstr.LogonHandler.ExpiryTime))
                {
                    authInstr.TryToLogonAsync(delegate (AuthorizedInstruction _)
                    {
                        // Send query using current token.
                        SendQuery(authInstr.AuthorizedToken);
                    },
                    TerminationToken);
                }
                else
                {
                    // Send query using current token.
                    SendQuery(authInstr.AuthorizedToken);
                }


                void SendQuery(string token)
                {
                    // Build personalized query.
                    string informQuery =
                        baseQuery + UniformQueries.API.SPLITTING_SYMBOL +
                        "token=" + token;

                    // Sending query to inform related servers about event.
                    InformAuthorityFollowers(informQuery);
                }
            }
        }
        #endregion
    }
}
