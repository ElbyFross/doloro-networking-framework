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
        public string guestChanel = "guest";

        /// <summary>
        /// Requesting reciving token on target server with provided auth params.
        /// </summary>
        public void RequestToken()
        {
            string guestToken = null;

            // Request logon.
            LogonHandler.TryToLogonAsync(
                guestToken,
                authLogin,
                authPassword,
                routingIP,
                pipeName);
        }

        /// <summary>
        /// Handler
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
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
        [System.Xml.Serialization.XmlIgnore]
        private USER_LOGON.LogonProcessor _LogonHandler;
    }
}
