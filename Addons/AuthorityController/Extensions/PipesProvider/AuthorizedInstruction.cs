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
using System.Xml.Serialization;
using UniformQueries;
using System.Threading;
using System.Threading.Tasks;
using PipesProvider.Networking.Routing;
using AuthorityController.Queries;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// Provide data and API required for connections that require authorization as Authority Controller user.
    /// </summary>
    public class AuthorizedInstruction : Instruction
    {
        #region Public fields
        /// <summary>
        /// Login for user authentification in AuthorityController on instruction's target server.
        /// </summary>
        public string authLogin;

        /// <summary>
        /// Password for user authentification in AuthorityController on instruction's target server.
        /// </summary>
        public string authPassword;

        /// <summary>
        /// Name of the broadcasting pipe that providing guest tokens.
        /// </summary>
        public string guestChanel = "guests";
        #endregion

        #region Public properties
        /// <summary>
        /// Return token authorized on target server by using provided data.
        /// </summary>
        public string AuthorizedToken
        {
            get { return LogonHandler.Token; }
        }

        /// <summary>
        /// Return token authorized on target server as guest.
        /// </summary>
        public string GuestToken
        {
            get { return GuestTokenHandler.Token; }
        }
        #endregion

        #region API
        /// <summary>
        /// Tring to recive token authorized in authority controller of target server.
        /// </summary>
        /// <param name="callback">Delegate that will be called when logon operation would be finished.</param>
        /// <param name="cancellationToken">Using this token you can terminate task.</param>
        public async void TryToLogonAsync(
            System.Action<AuthorizedInstruction> callback, 
            CancellationToken cancellationToken)
        {
            // Start new logon task.
            await Task.Run(() =>
            {
                // Request logon.
                TryToLogon();

                // Call callback.
                callback?.Invoke(this);
            },
            cancellationToken);
        }

        /// <summary>
        /// Tring to recive token authorized in authority controller of target server.
        /// </summary>
        public bool TryToLogon()
        {
            bool asyncOperationStarted = false;

            #region Guest token processing
            // Is the guest token is relevant.
            bool guestTokenValid =
                string.IsNullOrEmpty(GuestTokenHandler.Token) ||
                AuthorityController.API.Tokens.IsExpired(GuestTokenHandler.Token);

            if (!guestTokenValid)
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
                    pipeName);
            }

            // Wait for guest token.
            while (asyncOperationStarted)
            {
                Thread.Sleep(100);
            }

            // Drop if guest token not recived.
            if (string.IsNullOrEmpty(GuestTokenHandler.Token))
            {
                return false;
            }
            #endregion

            #region Logon processing
            // Lock thread.
            asyncOperationStarted = true;

            // Callback that will be call whenlogon would be finished.
            LogonHandler.ProcessingFinished += LogonFinishedCallback;
            void LogonFinishedCallback(QueryProcessor _, bool result, object message)
            {
                // Unsubscribe from handler.
                LogonHandler.ProcessingFinished -= LogonFinishedCallback;

                // Unlock thread.
                asyncOperationStarted = false;
            }

            // Request logon.
            LogonHandler.TryToLogonAsync(
                GuestTokenHandler.Token,
                authLogin,
                authPassword,
                routingIP,
                pipeName);

            // Wait for guest token.
            while (asyncOperationStarted)
            {
                Thread.Sleep(100);
            }

            // Drop if guest token not recived.
            if (string.IsNullOrEmpty(LogonHandler.Token))
            {
                return false;
            }
            #endregion

            return true;
        }
        #endregion

        #region Handlers
        /// <summary>
        /// Handler that take full control on logon process.
        /// </summary>
        [XmlIgnore]
        private USER_LOGON.LogonProcessor LogonHandler
        {
            get
            {
                // Create new if not started yet.
                if (_LogonHandler == null)
                {
                    _LogonHandler = new USER_LOGON.LogonProcessor();
                }

                return _LogonHandler;
            }
        }

        [XmlIgnore]
        private USER_LOGON.LogonProcessor _LogonHandler;

        /// <summary>
        /// Handler that take full control on reciving of guest token.
        /// </summary>
        [XmlIgnore]
        private GET_GUEST_TOKEN.GuestTokenProcessor GuestTokenHandler
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

        [XmlIgnore]
        private GET_GUEST_TOKEN.GuestTokenProcessor _GuestTokenHandler;
        #endregion
    }
}
