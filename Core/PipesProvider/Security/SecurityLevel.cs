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
    /// Anonymous - not require logon. 
    /// Require Guest user on server.
    /// Require allownce to network access via Guest accounts.
    /// 
    /// RemoteLogon - Require authentication via one of the profile on server.
    /// 
    /// Local - Pipe will be accessed only on the local machine.
    /// 
    /// Administrator - access to pipe will provided only for administrators. By default allowed via remote authentication.
    /// 
    /// Internal - pipe will controlled only be server application and system.
    /// Any external coonection will be blocked.
    /// </summary>
    public enum SecurityLevel
    {
        Anonymous = 2,
        RemoteLogon = 4,
        Local = 8,
        Administrator = 16,
        Internal = 32
    }
}
