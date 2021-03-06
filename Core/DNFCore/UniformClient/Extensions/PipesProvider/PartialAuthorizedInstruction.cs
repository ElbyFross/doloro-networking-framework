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
using System.Xml.Serialization;
using UniformQueries;
using System.Threading;
using System.Threading.Tasks;
using PipesProvider.Networking.Routing;
using BaseQueries;
using UniformQueries.Executable;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// Provides a data and an API required for connections that demand partical authorization rights on a server.
    /// </summary>
    [Serializable]
    public class PartialAuthorizedInstruction : Instruction
    {
        #region Public fields
        /// <summary>
        /// Name of the broadcasting pipe that providing guest tokens.
        /// </summary>
        public string guestChanel = "guests";
        #endregion

        #region Public properties
        /// <summary>
        /// Return token authorized on target server as guest.
        /// </summary>
        [XmlIgnore]
        public string GuestToken
        {
            get
            {
                if(!IsPartialAuthorized)
                {
                    TryToGetGuestToken(CancellationToken.None);
                }
                return GuestTokenHandler.Token;
            }
        }

        /// <summary>
        /// Check does instruction has a guest authorization.
        /// </summary>
        [XmlIgnore]
        public bool IsPartialAuthorized
        {
            get
            {
                return GuestTokenHandler.IsAutorized;
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Tring to recive partial authorized token from target server.
        /// </summary>
        /// <param name="cancellationToken">Using this token you can terminate task.</param>
        /// <returns>Result of operation.</returns>
        public async Task<bool> TryToGetGuestTokenAsync(CancellationToken cancellationToken)
        {
            bool result = false;

            // Start new logon task.
            await Task.Run(() =>
            {
                // Request logon.
                result = TryToGetGuestToken(cancellationToken);
            },
            cancellationToken);

            return result;
        }

        /// <summary>
        /// Tring to recive partial authorized token from target server.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to termination of the logon process.</param>
        /// <returns></returns>
        public bool TryToGetGuestToken(CancellationToken cancellationToken)
        {
            bool asyncOperationStarted = false;
            
            #region Guest token processing
            // Is the guest token is relevant.
            bool guestTokenInvalid =
                string.IsNullOrEmpty(GuestTokenHandler.Token) ||
                Tokens.IsExpired(GuestTokenHandler.Token, GuestTokenHandler.ExpiryTime);

            if (guestTokenInvalid)
            {
                // Lock thread.
                asyncOperationStarted = true;

                // Callback that will be call when guest token would be recived.
                GuestTokenHandler.ProcessingFinished += GuestTokenRecivedCallback;
                void GuestTokenRecivedCallback(QueryProcessor _, bool result, object message)
                {
                    // Unsubscribe from handler.
                    GuestTokenHandler.ProcessingFinished -= GuestTokenRecivedCallback;

                    // Unlock thread.
                    asyncOperationStarted = false;
                }

                // Recive guest token to get access to server.
                GuestTokenHandler.TryToReciveTokenAsync(
                    routingIP,
                    guestChanel,
                    cancellationToken);
            }

            // Wait for guest token.
            while (asyncOperationStarted)
            {
                Thread.Sleep(50);
            }

            // Drop if guest token not recived.
            if (string.IsNullOrEmpty(GuestTokenHandler.Token))
            {
                Console.WriteLine(routingIP + "/" + pipeName + ": GUEST TOKEN NOT RECEIVED");
                return false;
            }
            #endregion

            Console.WriteLine(routingIP + "/" + pipeName + ": GUEST TOKEN RECEIVED: " + GuestTokenHandler.Token);
            return true;
        }
        #endregion

        #region Handlers
        /// <summary>
        /// Handler that take full control on reciving of guest token.
        /// </summary>
        [XmlIgnore]
        public GET_GUEST_TOKEN.GuestTokenProcessor GuestTokenHandler
        {
            get
            {
                // Create new if not started yet.
                if (_GuestTokenHandler == null)
                {
                    _GuestTokenHandler = new GET_GUEST_TOKEN.GuestTokenProcessor();
                }

                return _GuestTokenHandler;
            }
        }

        /// <summary>
        /// Handler that take full control on reciving of guest token.
        /// </summary>
        [XmlIgnore]
        protected GET_GUEST_TOKEN.GuestTokenProcessor _GuestTokenHandler;
        #endregion
    }
}
