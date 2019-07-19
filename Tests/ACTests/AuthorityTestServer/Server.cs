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
using UniformServer;

namespace ACTests.AuthorityTestServer
{
    public class Server : BaseServer
    {
        /// <summary>
        /// Starting server that would listen authority queries.
        /// </summary>
        /// <param name="pipeName"></param>
        /// <returns></returns>
        public static Server StartQueryProcessing(string pipeName)
        {
            // Instiniate server.
            Server serverBufer = new Server
            {
                pipeName = pipeName
            };

            // Starting server loop.
            serverBufer.StartServerThread(
                Guid.NewGuid().ToString(), serverBufer,
                ThreadingServerLoop_PP_Input);

            return serverBufer;
        }
    }
}
