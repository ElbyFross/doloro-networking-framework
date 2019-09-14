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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipesProvider.Security
{
    /// <summary>
    /// Contaier that contain logon data for remote machine.
    /// </summary>
    [System.Serializable]
    public struct LogonConfig
    {
        /// <summary>
        /// Name of user for remote logon.
        /// </summary>
        public string userName;

        /// <summary>
        /// Password to remote user.
        /// </summary>
        public string password;

        /// <summary>
        /// Domain name of target machine.
        /// "Workgroup" as example.
        /// </summary>
        public string domain;

        /// <summary>
        /// Validate data and decide is this user has enought information to impersonate.
        /// </summary>
        public bool IsAnonymous
        {
            get
            {
                if (string.IsNullOrEmpty(userName))
                    return true;

                //if (string.IsNullOrEmpty(password))
                //    return true;

                //if (string.IsNullOrEmpty(domain))
                //    return true;

                return false;
            }
        }

        /// <summary>
        /// Return anonymous logon params.
        /// </summary>
        public static LogonConfig Anonymous
        {
            get
            {
                return new LogonConfig()
                {
                    domain = System.Environment.MachineName,
                    userName = "",
                    password = ""
                }; 
            }
        }

        /// <summary>
        /// Instiniate logon config settings.
        /// </summary>
        /// <param name="userName">Name of the registred user.</param>
        /// <param name="password">user's password.</param>
        /// <param name="domain">Machine's network domain.</param>
        public LogonConfig(string userName, string password, string domain)
        {
            this.userName = userName;
            this.password = password;
            this.domain = domain; 
        }
    }
}
