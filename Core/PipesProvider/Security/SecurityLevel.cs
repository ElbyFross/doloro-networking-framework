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
    /// Defines requirements for connection establishing.
    /// </summary>
    public enum SecurityLevel
    {
        /// <summary>
        /// Not demands logon.
        /// Demands a Guest user on a server device.
        /// Demands allownce to a network access via a Guest accounts.
        /// </summary>
        Anonymous = 2,
        /// <summary>
        /// Requires authentication via one of the profile on server.
        /// </summary>
        RemoteLogon = 4,
        /// <summary>
        /// A pipe will be available only at the local machine.
        /// </summary>
        Local = 8,
        /// <summary>
        /// An access to a pipe will provided only for administrators. By default allowed via remote authentication.
        /// </summary>
        Administrator = 16,
        /// <summary>
        /// A pipe will controlled only by a server application and system. Any external connection will be blocked.
        /// </summary>
        Internal = 32
    }
}
