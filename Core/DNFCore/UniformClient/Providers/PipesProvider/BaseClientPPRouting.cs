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
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

using PipesProvider.Networking.Routing;
using PipesProvider.Client;

namespace UniformClient
{
    /// <summary>
    /// Part off class that provide API to work by routing tables.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Table that contain instruction that allow to determine the server which is a target for recived query.
        /// </summary>
        public static RoutingTable routingTable;

        /// <summary>
        /// Update routing table by the files that will found be requested directory.
        /// Also auto loking for core routing  table by "resources\routing\".
        /// 
        /// In case if tables not found then create new one to provide example.
        /// </summary>
        /// <param name="directories"></param>
        public static void LoadRoutingTables(params string[] directories)
        {
            #region Load routing tables
            // Load routing tables
            routingTable = null;
            // From system folders.
            routingTable += RoutingTable.LoadRoutingTables(AppDomain.CurrentDomain.BaseDirectory + "resources\\routing\\", SearchOption.AllDirectories);
            // From custrom directories.
            foreach (string dir in directories)
            {
                routingTable += RoutingTable.LoadRoutingTables(dir, SearchOption.AllDirectories);
            }
            #endregion

            #region Request public keys
            foreach (Instruction instruction in routingTable.intructions)
            {
                // If encryption requested.
                if (instruction.RSAEncryption)
                {
                    Console.WriteLine("INSTRUCTION ROUTING RSA", instruction.routingIP, instruction.pipeName);

                    // Request public key reciving.
                    GetValidPublicKeyViaPP(instruction);
                }
            }
            #endregion

            #region Validate
            // If routing table not found.
            if (routingTable.intructions.Count == 0)
            {
                // Log error.
                Console.WriteLine("ROUTING TABLE NOT FOUND: Create default table by directory \\resources\\routing\\ROUTING.xml");

                // Set default intruction.
                routingTable.intructions.Add(Instruction.Default);

                // Save sample routing table to application files.
                RoutingTable.SaveRoutingTable(routingTable, AppDomain.CurrentDomain.BaseDirectory + "resources\\routing\\", "ROUTING");
            }
            else
            {
                // Log error.
                Console.WriteLine("ROUTING TABLE: Detected {0} instructions.", routingTable.intructions.Count);
            }
            #endregion
        }
    }
}
