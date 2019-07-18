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
using PipesProvider.Server;
using UQAPI = UniformQueries.API;
using PipesProvider.Server.TransmissionControllers;

namespace PipesProvider.Handlers
{
    /// <summary>
    /// Handlers that provide transmission between serve and clients.
    /// </summary>
    public static class DNS
    {
        /// <summary>
        /// Code that will work on server loop when connection will be established.
        /// Recoomended to using as default DNS Handler for queries reciving.
        /// </summary>
        public static async void ClientToServerAsync(BaseServerTransmissionController controller)
        {
            // Open stream reader.
            StreamReader sr = new StreamReader(controller.pipeServer);
            string queryBufer;
            DateTime sessionTime = DateTime.Now.AddSeconds(5000);

            // Read until trasmition exits not finished.
            while (controller.pipeServer.IsConnected)
            {
                #region Reciving message
                queryBufer = null;
                // Read line from stream.
                while (queryBufer == null)
                {
                    // Avoid an error caused to disconection of client.
                    try
                    {
                        queryBufer = await sr.ReadToEndAsync();
                    }
                    // Catch the Exception that is raised if the pipe is broken or disconnected.
                    catch (Exception e)
                    {
                        Console.WriteLine("DNS HANDLER ERROR: {0}", e.Message);
                        return;
                    }

                    if (DateTime.Compare(sessionTime, DateTime.Now) < 0)
                    {
                        Console.WriteLine("Connection terminated cause allowed time has expired.");
                        /// Avoid disconnectin error.
                        try
                        {
                            controller.pipeServer.Disconnect();
                        }
                        catch { throw; }

                        return;
                    }
                }
                #endregion

                #region Finalizing connection
                // Disconnect user if query recived.
                if (controller.pipeServer.IsConnected)
                {
                    controller.pipeServer.Disconnect();
                }

                // Remove temporal data.
                controller.pipeServer.Dispose();
                #endregion

                #region Query processing
                // Drop if stream is over.
                if (string.IsNullOrEmpty(queryBufer))
                {
                    //Console.WriteLine("NULL REQUEST AVOIDED. CONNECTION TERMINATED.");
                    break;
                }

                // Try to decrypt. In case of fail decryptor return entry message.
                queryBufer = Security.Crypto.DecryptString(queryBufer);

                // Log query.
                Console.WriteLine(@"RECIVED QUERY (DNS0): {0}", queryBufer);

                // Try to get correct transmisssion controller.
                if (controller is ClientToServerTransmissionController ct)
                {
                    // Redirect handler.
                    ct.queryHandlerCallback?.Invoke(controller, queryBufer);
                }
                else
                {
                    // Log error.
                    Console.WriteLine(@"DNS HANDLER ERROR (DNS40):\nQuery processing not posssible.\nTransmission controller not {0}",
                        typeof(ClientToServerTransmissionController).FullName);
                }
                #endregion
            }

            // Log about transmission finish.
            Console.WriteLine("TRANSMISSION FINISHED AT {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }

        /// <summary>
        /// Code that will work on server loop when connection will be established.
        /// Recoomended to using as default DNS Handler for message sending.
        /// </summary>
        public static async void ServerToClientAsync(BaseServerTransmissionController controller)
        {
            // Try to get correct controller.
            if (controller is ServerToClientTransmissionController outController)
            {
                // Open stream reader.
                StreamWriter sw = new StreamWriter(outController.pipeServer);

                // Buferise query before calling of async operations.
                string sharedQuery = outController.ProcessingQuery;

                // Read until trasmition exits not finished.
                // Avoid an error caused to disconection of client.
                try
                {
                    // Write message to stream.
                    Console.WriteLine("{0}: Start transmission to client.", outController.pipeName);
                    await sw.WriteAsync(sharedQuery);
                    await sw.FlushAsync();
                }
                // Catch the Exception that is raised if the pipe is broken or disconnected.
                catch (Exception e)
                {
                    Console.WriteLine("DNS HANDLER ERROR (StC0): {0}", e.Message);
                    return;
                }
            }
            else
            {
                // Log about transmission finish.
                Console.WriteLine("TRANSMISSION ERROR: Try to user invalid controller as output.");
            }

            // Disconnect user if query recived.
            if (controller.pipeServer.IsConnected)
            {
                try
                {
                    controller.pipeServer.Disconnect();
                }
                catch
                { }
            }

            // Remove temporal data.
            controller.pipeServer.Dispose();

            // Stop this transmission line.
            controller.SetStoped();

            // Log about transmission finish.
            Console.WriteLine("TRANSMISSION FINISHED AT {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }

        /// <summary>
        /// Code that will work on server loop when connection will be established.
        /// Recoomended to using as default DNS Handler for message sending.
        /// 
        /// Provide broadcasting to client by useing GetMessage delegate of BroadcastingServerTC controller.
        /// </summary>
        public static async void ServerBroadcasting(BaseServerTransmissionController controller)
        {
            // Try to get correct controller.
            if (controller is BroadcastingServerTransmissionController broadcastController)
            {
                // Open stream reader.
                StreamWriter sw = new StreamWriter(controller.pipeServer);

                // Read until trasmition exits not finished.
                // Avoid an error caused to disconection of client.
                try
                {
                    // Get message
                    string message = broadcastController?.GetMessage();

                    // Write message to stream.
                    Console.WriteLine("{0}: Start transmission to client.", controller.pipeName);
                    await sw.WriteAsync(message);
                    await sw.FlushAsync();
                }
                // Catch the Exception that is raised if the pipe is broken or disconnected.
                catch (Exception e)
                {
                    Console.WriteLine("DNS HANDLER ERROR (StC0): {0}", e.Message);
                    return;
                }
            }
            else
            {
                // Log about transmission finish.
                Console.WriteLine("TRANSMISSION ERROR: Try to user invalid controller for broadcasting.");
            }

            // Disconnect user if query recived.
            if (controller.pipeServer.IsConnected)
            {
                controller.pipeServer.Disconnect();
            }

            // Log about transmission finish.
            Console.WriteLine("TRANSMISSION FINISHED AT {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }
    }
}
