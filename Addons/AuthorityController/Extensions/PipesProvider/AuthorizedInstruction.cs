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
using UniformQueries.Executable;
using AuthorityController.Data.Application;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// Provides a data and an API required for connection that requires authorization as an Authority Controller user.
    /// </summary>
    [Serializable]
    public class AuthorizedInstruction : PartialAuthorizedInstruction
    {
        #region Public fields
        /// <summary>
        /// Login for user authentification in AuthorityController on instruction's target server.
        /// </summary>
        public string authLogin = null;

        /// <summary>
        /// Password for user authentification in AuthorityController on instruction's target server.
        /// </summary>
        public string authPassword = null;
        #endregion

        #region Public properties
        /// <summary>
        /// Return token authorized on target server by using provided data.
        /// </summary>
        [XmlIgnore]
        public string AuthorizedToken
        {
            get { return LogonHandler.Token; }
        }

        /// <summary>
        /// Check does instruction has the full authorization.
        /// </summary>
        [XmlIgnore]
        public bool IsFullAuthorized
        {
            get
            {
                return LogonHandler.IsAutorized;
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Trying to recive token authorized in authority controller of target server.
        /// </summary>
        /// <param name="callback">Delegate that will be called when logon operation would be finished. Return bool result of operation.</param>
        /// <param name="cancellationToken">Using this token you can terminate task.</param>
        public async void TryToLogonAsync(
            Action<AuthorizedInstruction, bool> callback, 
            CancellationToken cancellationToken)
        {
            // Start new logon task.
            await Task.Run(() =>
            {
                // Request logon.
                bool restult = TryToLogon();

                // Call callback.
                callback?.Invoke(this, restult);
            },
            cancellationToken);
        }


        /// <summary>
        /// Tring to recive token authorized in authority controller of target server.
        /// </summary>
        public bool TryToLogon()
        {
            return TryToLogon(AuthorityController.Session.Current.TerminationTokenSource.Token);
        }

        /// <summary>
        /// Tring to recive token authorized in authority controller of target server.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to termination of the logon process.</param>
        /// <returns></returns>
        public bool TryToLogon(CancellationToken cancellationToken)
        {
            // Drop if already started.
            if (GuestTokenHandler.IsInProgress)
            {
                return false;
            }

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
                    pipeName,
                    cancellationToken);
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
        public USER_LOGON.LogonProcessor LogonHandler
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

        /// <summary>
        /// Handler that take full control on logon process.
        /// </summary>
        [XmlIgnore]
        protected USER_LOGON.LogonProcessor _LogonHandler;
        #endregion
    }
}
