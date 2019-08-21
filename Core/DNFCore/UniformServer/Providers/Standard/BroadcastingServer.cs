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

using PipesProvider.Server.TransmissionControllers;

namespace UniformServer.Standard
{
    /// <summary>
    /// Server that allow instiniate BaseServer.
    /// Not contain any additive methods.
    /// 
    /// Case of using - simple operations like registing of server for answer.
    /// </summary>
    public class BroadcastingServer : BaseServer
    {
        public BroadcastingServerTransmissionController.MessageHandeler GetMessage;

        // Init default constructor.
        public BroadcastingServer() : base()
        {

        }
    }
}
