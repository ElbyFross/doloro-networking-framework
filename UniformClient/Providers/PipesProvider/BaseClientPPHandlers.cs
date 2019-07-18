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
    /// Par of classs that profide transmisssion handlers.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Handler that will recive message from the server.
        /// </summary>
        /// <param name="sharedObject">
        /// Normaly is a TransmissionLine that contain information about actual transmission.</param>
        public static async void HandlerInputTransmissionAsync(object sharedObject)
        {
            // Drop as invalid in case of incorrect transmitted data.
            if (!(sharedObject is TransmissionLine lineProcessor))
            {
                Console.WriteLine("TRANSMISSION ERROR (UQPP0): INCORRECT TRANSFERD DATA TYPE. PERMITED ONLY \"LineProcessor\"");
                return;
            }

            // Mark line as busy to avoid calling of next query, cause this handler is async.
            lineProcessor.Processing = true;

            #region Reciving message
            // Open stream reader.
            string message = null;
            StreamReader sr = new StreamReader(lineProcessor.pipeClient);
            try
            {
                Console.WriteLine("{0}/{1}: WAITING FOR MESSAGE",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);

                while (string.IsNullOrEmpty(message))
                {
                    // Avoid an error caused to disconection of client.
                    try
                    {
                        message = await sr.ReadToEndAsync();
                    }
                    // Catch the Exception that is raised if the pipe is broken or disconnected.
                    catch (Exception e)
                    {
                        // Log error.
                        Console.WriteLine("DNS HANDLER ERROR (USAH0): {0}", e.Message);

                        // Stop processing merker to pass async block.
                        lineProcessor.Processing = false;

                        // Close processor case this line already deprecated on the server side as single time task.
                        lineProcessor.Close();
                        return;
                    }
                }
            }
            // Catch the Exception that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                // Log
                if (string.IsNullOrEmpty(message))
                {
                    Console.WriteLine("DNS HANDLER ERROR ({1}): {0}", e.Message, lineProcessor.pipeClient.GetHashCode());
                }
            }
            #endregion

            if (!string.IsNullOrEmpty(message))
            {
                // Log state.
                Console.WriteLine("{0}/{1}: MESSAGE RECIVED",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);

                #region Processing message
                // Try to call answer handler.
                string tableGUID = lineProcessor.ServerName + "\\" + lineProcessor.ServerPipeName;
                // Look for delegate in table.
                if (DuplexBackwardCallbacks[tableGUID] is
                    System.Action<TransmissionLine, object> registredCallback)
                {
                    if (registredCallback != null)
                    {
                        // Invoke delegate if found and has dubscribers.
                        registredCallback.Invoke(lineProcessor, message);
                    }
                    else
                    {
                        Console.WriteLine("{0}/{1}: ANSWER CALLBACK HAS NO SUBSCRIBERS",
                            lineProcessor.ServerName, lineProcessor.ServerPipeName);
                    }
                }
                else
                {
                    Console.WriteLine("{0}/{1}: ANSWER HANDLER NOT FOUND BY {2}",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName, tableGUID);
                }

                //Console.WriteLine("{0}/{1}: ANSWER READING FINISH.\nMESSAGE: {2}",
                //    lineProcessor.ServerName, lineProcessor.ServerPipeName, message);
                #endregion
            }

            // Stop processing merker to pass async block.
            lineProcessor.Processing = false;

            // Close processor case this line already deprecated on the server side as single time task.
            lineProcessor.Close();
        }

        /// <summary>
        /// Handler that send last dequeued query to server when connection will be established.
        /// </summary>
        /// <param name="sharedObject">Normaly is a TransmissionLine that contain information about actual transmission.</param>
        public static async void HandlerOutputTransmisssionAsync(object sharedObject)
        {
            // Drop as invalid in case of incorrect transmitted data.
            if (!(sharedObject is PipesProvider.Client.TransmissionLine lineProcessor))
            {
                Console.WriteLine("TRANSMISSION ERROR (UQPP0): INCORRECT TRANSFERED DATA TYPE. PERMITED ONLY \"LineProcessor\"");
                return;
            }

            string sharableQuery = lineProcessor.LastQuery.Query;

            // If requested encryption.
            if (lineProcessor.RoutingInstruction != null &&
                lineProcessor.RoutingInstruction.RSAEncryption)
            {
                // Check if instruction key is valid.
                // If key expired or invalid then will be requested new.
                if (!lineProcessor.RoutingInstruction.IsValid)
                {
                    // Request new key.
                    UniformClient.BaseClient.GetValidPublicKeyViaPP(lineProcessor.RoutingInstruction);

                    // Log.
                    Console.WriteLine("WAITING FOR PUBLIC RSA KEY FROM {0}/{1}",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);

                    // Wait until validation time.
                    // Operation will work in another threads, so we just need to take a time.
                    while (!lineProcessor.RoutingInstruction.IsValid)
                    {
                        Thread.Sleep(50);
                    }
                }

                // Encrypt query by public key of target server.
                sharableQuery = PipesProvider.Security.Crypto.EncryptString(sharableQuery, lineProcessor.RoutingInstruction.PublicKey);
            }

            // Open stream writer.
            StreamWriter sw = new StreamWriter(lineProcessor.pipeClient);
            try
            {
                await sw.WriteAsync(sharableQuery);
                await sw.FlushAsync();
                Console.WriteLine("TRANSMITED: {0}", lineProcessor.LastQuery);
                //sw.Close();
            }
            // Catch the Exception that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.WriteLine("DNS HANDLER ERROR ({1}): {0}", e.Message, lineProcessor.pipeClient.GetHashCode());

                // Retry transmission.
                if (lineProcessor.LastQuery.Attempts < 10)
                {
                    // Add to queue.
                    lineProcessor.EnqueueQuery(lineProcessor.LastQuery);

                    // Add attempt.
                    lineProcessor++;
                }
                else
                {
                    // If transmission attempts over the max count.
                }
            }

            // Unlock loop.
            lineProcessor.Processing = false;
        }
    }
}
