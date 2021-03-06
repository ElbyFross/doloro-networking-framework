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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACTests.Helpers
{
    /// <summary>
    /// Providing methods that simplifing work with network.
    /// </summary>
    public static class Networking
    {
        /// <summary>
        /// Name  of the pipe that would be started on server during tests.
        /// </summary>
        public readonly static string DefaultQueriesPipeName = "ACTestPublic";

        /// <summary>
        /// Name of the pipe that would be started on server during tests.
        /// </summary>
        public readonly static string DefaultGuestPipeName = "ACTestGuest";


        /// <summary>
        /// Starting public server that would able to recive queries.
        /// </summary>
        public static void StartPublicServer()
        {
            StartPublicServer(1);
        }

        /// <summary>
        /// Starting public server that would able to recive queries.
        /// </summary>
        /// <param name="chanels">Count of servers that will start.</param>
        public static void StartPublicServer(int chanels)
        {
            StartPublicServer(chanels, DefaultQueriesPipeName);
        }

        /// <summary>
        /// Starting public server that would able to recive queries.
        /// </summary>
        /// <param name="chanels">Count of servers that will start.</param>
        /// <param name="pipeName">Name of pipe that would started on servers.</param>
        public static void StartPublicServer(int chanels, string pipeName)
        {
            // Stop previos servers.
            PipesProvider.Server.ServerAPI.StopAllServers();

            // Start new server pipes.
            for (int i = 0; i < chanels; i++)
            {
                // Open server.
                AuthorityTestServer.Server.StartQueryProcessing(pipeName);
            }
        }
    }
}
