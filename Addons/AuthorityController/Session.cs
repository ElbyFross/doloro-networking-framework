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
    /// An instance of that class controls a data relative to a current authority session.
    /// </summary>
    [Serializable]
    public class Session
    {
        #region Public properties and fields
        /// <summary>
        /// A current active session.
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
        /// Routing table that contains instructions to access reletive servers
        /// that need to be informed about token events.
        /// 
        /// Before sharing a query still will check is the query suituable for that routing instruction.
        /// If you no need any filtring then just leave query patterns empty.
        /// </summary>
        public RoutingTable AuthorityFollowers { get; set; }

        /// <summary>
        /// Token source that provide possibility to terminate all stated tasks in this session.
        /// </summary>
        public CancellationTokenSource TerminationTokenSource
        {
            get
            {
                if(_terminationToken == null)
                {
                    _terminationToken = new CancellationTokenSource();
                }

                return _terminationToken;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Instiniates a session object.
        /// Sets it to the <see cref="Current"/> property.
        /// </summary>
        public Session()
        {
            // Set this session as active.
            Current = this;

            // Initialize Termination token.
            _ = TerminationTokenSource;
        }
        #endregion

        #region Private fields
        /// <summary>
        /// Object that contains the current session.
        /// </summary>
        private static Session _current;

        /// <summary>
        /// A bufer that contains a temination token.
        /// </summary>
        private CancellationTokenSource _terminationToken;

        /// <summary>
        /// A table that contains rights provided to tokens.
        ///
        /// Key - string token
        /// Value - TokenInfo
        /// </summary>
        private readonly Hashtable tokensInfo = new Hashtable();
        #endregion

        #region Public methods
        /// <summary>
        /// Creates token registration binded to a user profile.
        /// Not provided fields will filled like anonymous.
        /// A time stamp will contains the time of method call.
        /// </summary>
        /// <param name="user"> A user profile that contains core data.</param>
        /// <param name="token">A token bonded with the user.</param>
        /// <returns>A result of operation.</returns>
        public bool AssignTokenToUser(User user, string token)
        {
            return AssignTokenToUser(
                user,
                token,
                "anonymous",
                "anonymous",
                DateTime.Now.ToBinary().ToString());
        }

        /// <summary>
        /// Creates token registration binded to a user profile.
        /// </summary>
        /// <param name="user"> A user profile that contains core data.</param>
        /// <param name="token">A token bonded with the user.</param>
        /// <param name="mac">A MAC-adress of user machine.</param>
        /// <param name="os">An OS installed on the user machine</param>
        /// <param name="stamp">A time stamp that marks when the session was started.</param>
        /// <returns>A result of operation.</returns>
        public bool AssignTokenToUser(User user, string token, string mac, string os, string stamp)
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
        /// Sets rights' codes array as relative to some token.
        ///
        /// Makes and execute the <see cref="Queries.SET_TOKEN_RIGHTS"/> query to an every related server.
        /// Shares an information in anonymous format. Any personal data will droped.
        /// </summary>
        /// <param name="targetToken">A token that for rights update.</param>
        /// <param name="rights">An array of new rights provided to the token.</param>
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
        /// Tries to load rights provided to a token.
        /// </summary>
        /// <param name="token">A session token.</param>
        /// <param name="rights">An array of rights' codes related to the token.</param>
        /// <returns>Result of the search operation.</returns>
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
        /// Removs a token from the registred and informs relative servers about that.
        /// </summary>
        /// <param name="token">A token that will expired.</param>
        public bool SetExpired(string token)
        {
            if (RemoveToken(token))
            {
                // Compose query that will shared to related servers to update them local data.
                UniformQueries.Query informQuery = new UniformQueries.Query(
                    new UniformQueries.QueryPart("set"),
                    new UniformQueries.QueryPart("token", token),
                    new UniformQueries.QueryPart("expired"));

                // Send query to infrom related servers about event.
                InformAuthorityFollowers(informQuery);

                // Confirm token removing.
                return true;
            }

            // Inform that token not found.
            return false;
        }

        /// <summary>
        /// Tries to find a token info among registered ones.
        /// </summary>
        /// <param name="token">A token for search.</param>
        /// <param name="info">A toke info in case if found.</param>
        /// <returns>A result of search operation.</returns>
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
        /// Sends a message to all authority following servers described 
        /// in the <see cref="AuthorityFollowers"/> table.
        /// </summary>
        /// <param name="message">A message that will be shared.</param>
        public void InformAuthorityFollowers(UniformQueries.Query message)
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
        /// Removes a token from the <see cref="tokensInfo"/> table.
        /// </summary>
        /// <param name="token">A token for remove.</param>
        /// <returns>A result of operation.</returns>
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
        /// Sends new rights of the token to related servers.
        /// </summary>
        /// <param name="targetToken">A token to handle.</param>
        /// <param name="rights">Token's rights.</param>
        private void ShareTokenRights_MessageProcessor(string targetToken, string[] rights)
        {
            // Drop invalid or unnecessary  routing.
            if (AuthorityFollowers == null ||
                AuthorityFollowers.intructions == null ||
                AuthorityFollowers.intructions.Count == 0)
            {
                return;
            }

            // Build rights to string format.
            string rightsValue = "";
            foreach (string rightsCode in rights)
            {
                if(string.IsNullOrEmpty(rightsValue))
                {
                    rightsValue += "+";
                }
                rightsValue += rightsCode;
            }
            
            // Send query to every following server.
            foreach (AuthorizedInstruction authInstr in AuthorityFollowers.intructions)
            {
                // Relogon if not logon or token exipred.
                if (string.IsNullOrEmpty(authInstr.AuthorizedToken) ||
                    UniformQueries.Tokens.IsExpired(authInstr.AuthorizedToken, authInstr.LogonHandler.ExpiryTime))
                {
                    authInstr.TryToLogonAsync(delegate (AuthorizedInstruction _, bool restul)
                    {
                        if (restul)
                        {
                            // Send query using current token.
                            SendQuery(authInstr.AuthorizedToken);
                        }
                        else
                        {
                            Console.WriteLine("SESSION SHARING: Logon failed.");
                        }
                    },
                    TerminationTokenSource.Token);
                }
                else
                {
                    // Send query using current token.
                    SendQuery(authInstr.AuthorizedToken);
                }


                void SendQuery(string token)
                {
                    // Build query.
                    UniformQueries.Query query = new UniformQueries.Query(
                        new UniformQueries.QueryPart("set"),
                        new UniformQueries.QueryPart("targetToken", targetToken),
                        new UniformQueries.QueryPart("rights", rightsValue),
                        new UniformQueries.QueryPart("token", token));

                    // Sending query to inform related servers about event.
                    InformAuthorityFollowers(query);
                }
            }
        }
        #endregion
    }
}
