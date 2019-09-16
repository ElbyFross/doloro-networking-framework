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
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using UniformQueries;
using UQAPI = UniformQueries.API;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;


namespace PipesProvider.Handlers
{
    /// <summary>
    /// Service handlers that provide core processing and stability.
    /// </summary>
    public static class Service
    {
        /// <summary>
        /// Callback that will react on connection esstablishing.
        /// Will close waiting async operation and call shared delegate with server loop's code.
        /// </summary>
        /// <param name="result"></param>
        public static void ConnectionEstablishedCallbackRetranslator(IAsyncResult result)
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            // Load transmission meta data.
            BaseServerTransmissionController meta = (BaseServerTransmissionController)result.AsyncState;

            // Add tread name to simplify debuging.
            Thread.CurrentThread.Name = "ConnectionCallback " + meta.GetHashCode();

            // Stop connection waiting.
            try
            {
                // Trying to close connection if not closed.
                meta.pipeServer.EndWaitForConnection(meta.connectionMarker);
            }
            catch (Exception ex)
            {
                // Log if error caused not just by closed pipe.
                if (!(ex is ObjectDisposedException))
                {
                    Console.WriteLine("CONNECTION ERROR (CECR EWFC): {0} ", ex.Message);
                }
                else
                {
                    // Connection failed. Drop.
                    return;
                }
            }

            try
            {
                // Wait until connection.
                while(!meta.pipeServer.IsConnected)
                {
                    Thread.Sleep(5);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CONNECTION ERROR (CECR EWFC 2): {0} {1}", meta.pipeName, ex.Message);
            }

            //Console.WriteLine("\nAsync compleated:{0} {1}\nPipe connected:{2}\n", result.IsCompleted, result.CompletedSynchronously, meta.pipe.IsConnected);

            // Log about success.
            if (meta.pipeServer.IsConnected)
            {
                // Log connection.
                Console.WriteLine("\n{0}: Client connected. Handler: {1}", meta.pipeName, meta.connectionCallback != null ? meta.connectionCallback.Method.Name : "None");

                // Call connection handler.
                meta.connectionCallback?.Invoke(meta);
            }
            else
            {
                Console.WriteLine("\n{0}: Connection waiting was terminated", meta.pipeName);
            }
        }
    }
}
