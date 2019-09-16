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
    /// Part of class that provide API to oppening Client-Server transmisssion lines.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Object that useing to lock line establishing until operation finish.
        /// </summary>
        private static object lineLocker = new object();

        /// <summary>
        /// Automaticly create Transmission line or lokking for previos one.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="pipeName"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static TransmissionLine OpenTransmissionLineViaPP(
           string serverName,
           string pipeName,
           System.Action<TransmissionLine> callback)
        {
            SafeAccessTokenHandle token = System.Security.Principal.WindowsIdentity.GetAnonymous().AccessToken;
            string guid = serverName.GetHashCode() + "_" + pipeName.GetHashCode();
            return OpenTransmissionLineViaPP(new Standard.SimpleClient(), serverName, pipeName, ref token, callback);
        }

        /// <summary>
        /// Provide complex initalization of all relative systems. 
        /// Build meta data, regitrate line in common table.
        /// Start new thread to avoid freezes.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="token">Token that will be used for logon. on remote machine LSA. 
        /// Sharing by ref to allow update in internal lines.</param>
        /// <param name="serverName">Name of IP adress of remote or local server.</param>
        /// <param name="pipeName">Name of the pipe started on the server.</param>
        /// <param name="callback">Method that will be called when connection will be established.</param>
        /// <returns>Opened transmission line. Use line.Enqueue to add your query.</returns>
        public static TransmissionLine OpenTransmissionLineViaPP(
            BaseClient client,
            string serverName,
            string pipeName,
            ref SafeAccessTokenHandle token,
            System.Action<TransmissionLine> callback)
        {
            lock (lineLocker)
            {
                // Validate client.
                if (client == null)
                {
                    Console.WriteLine("CLIENT is NULL (BC_OTL_0). Unable to open new transmission line.");
                    return null;
                }

                // Get target GUID.
                string guid = TransmissionLine.GenerateGUID(serverName, pipeName);

                //// Try to load  trans line by GUID.
                //ClientAPI.TryGetTransmissionLineByGUID(guid, out TransmissionLine trnsLine);

                //if (trnsLine != null &&
                //    trnsLine.Direction == TransmissionLine.TransmissionDirection.Out)
                if(ClientAPI.TryGetTransmissionLineByGUID(guid, out TransmissionLine trnsLine))
                {
                    // If not obsolterd transmission line then drop operation.
                    if (!trnsLine.Closed)
                    {
                        Console.WriteLine("OTL {0} | FOUND", guid);
                        return trnsLine;
                    }
                    else
                    {
                        // Unregister line and recall method.
                        ClientAPI.TryToUnregisterTransmissionLine(guid);

                        Console.WriteLine("OTL {0} | RETRY", guid);

                        // Retry.
                        return OpenTransmissionLineViaPP(client, serverName, pipeName, ref token, callback);
                    }
                }
                // If full new pipe.
                else
                {
                    // Create new if not registred.
                    trnsLine = new TransmissionLine(
                        serverName,
                        pipeName,
                        callback,
                        ref token);

                    // Request thread start but let a time quantum only when this thread will pass order.
                    _ = StartPPClientThreadAsync(client, guid, trnsLine);
                }

                // Return oppened line.
                return trnsLine;
            }
        }
    }
}
