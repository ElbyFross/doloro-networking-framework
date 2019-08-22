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
using BaseQueries;
using UniformQueries.Executable;
using PipesProvider.Security;
using PipesProvider.Server;
using PipesProvider.Client;
using PipesProvider.Networking.Routing;
using PipesProvider.Server.TransmissionControllers;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// Instruction that allow retranslate broadcasting via servers chain.
    /// </summary>
    public class RelayInstruction : Instruction
    {
        /// <summary>
        /// Name of pipe started on server that would relay broadcasting from target server.
        /// Target server must be broadcasting one.
        /// 
        /// Shame:
        /// client -> relay-server-ip.entryPipeName -> routingIp.pipeName
        /// </summary>
        public string entryPipeName = "broadcasting";
    }
}
