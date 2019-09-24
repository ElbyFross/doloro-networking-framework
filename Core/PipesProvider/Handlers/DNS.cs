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
using PipesProvider.Security.Encryption;

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
            DateTime sessionTime = DateTime.Now.AddSeconds(5000);

            // Read until trasmition exits not finished.
            while (controller.pipeServer.IsConnected)
            {
                // Open stream reader.
                byte[] binaryQuery;
                #region Reciving message
                // Read data from stream.
                // Avoid an error caused to disconection of client.
                try
                {
                    binaryQuery = await UniformDataOperator.Binary.BinaryHandler.StreamReaderAsync(controller.pipeServer);
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
                    // Avoid disconnectin error.
                    try
                    {
                        controller.pipeServer.Disconnect();
                    }
                    catch
                    {
                        // Exception caused by disconecction on client side.
                    }

                    return;
                }
                #endregion

                #region Finalizing connection
                // Disconnect user if query recived.
                if (controller.pipeServer.IsConnected)
                {
                    try
                    {
                        controller.pipeServer.Disconnect();
                    }
                    catch
                    {
                        // Exception caused by disconecction on client side.
                    }
                }

                // Remove temporal data.
                controller.pipeServer.Dispose();
                #endregion

                #region Query processing
                // Drop if stream is over.
                if (binaryQuery == null)
                {
                    //Console.WriteLine("NULL REQUEST AVOIDED. CONNECTION TERMINATED.");
                    break;
                }

                // Decode query from binary data.
                Query query;
                try
                {
                    query = UniformDataOperator.Binary.BinaryHandler.FromByteArray<UniformQueries.Query>(binaryQuery);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("DNS HANDLER ERROR: DAMAGED DATA : {0}", ex.Message);
                    return;
                }

                // Try to decrypt. In case of fail decryptor return entry message.
                EnctyptionOperatorsHandler.EncryptionMeta encryptionMeta = 
                    EnctyptionOperatorsHandler.TryToDecrypt(ref query);

                // Log query.
                Console.WriteLine(@"RECIVED QUERY (DNS0): {0}", query);

                // Try to get correct transmisssion controller.
                if (controller is ClientToServerTransmissionController CToSTS)
                {
                    // Redirect handler.
                    CToSTS.queryHandlerCallback?.Invoke(controller, query);
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
                BinaryWriter sw = new BinaryWriter(outController.pipeServer);

                // Buferise query before calling of async operations.
                UniformQueries.Query sharedQuery = outController.ProcessingQuery;
                
                // Read until trasmition exits not finished.
                // Avoid an error caused to disconection of client.
                try
                {
                    // Wait until connection.
                    if (!outController.pipeServer.IsConnected)
                    {
                        Thread.Sleep(5);
                    }

                    // Write message to stream.
                    Console.WriteLine("{0}: Start transmission to client.", outController.pipeName);

                    // Getting data to sharing.
                    byte[] sharingData = UniformDataOperator.Binary.BinaryHandler.ToByteArray(sharedQuery);
                    int byteIndex = 0;


                    // Sending information about data size.
                    await new Task(delegate ()
                    { 
                        sw.Write(UniformDataOperator.Binary.BinaryHandler.ToByteArray<int>(sharingData.Length));
                        sw.Flush();
                    });

                    // Sending data to client.
                    await new Task(delegate ()
                    {
                        // Fill block.
                        for (int i = 0; i < 1024; i++)
                        {
                            // Drop if stream ended.
                            if(byteIndex >= sharingData.Length)
                            {
                                break;
                            }
                            sw.Write(sharingData[byteIndex]);
                            byteIndex++;
                        }
                        // Send block to destination.
                        sw.Flush();
                    });
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
                {
                    // Exception caused by disconecction on client side.
                }
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
                // Read until trasmition exits not finished.
                // Avoid an error caused to disconection of client.
                try
                {
                    // Get message
                    byte[] message = broadcastController?.GetMessage(broadcastController);

                    // Write message to stream.
                    Console.WriteLine("{0}: Start transmission to client.", controller.pipeName);

                    // Send data to stream.
                    await UniformDataOperator.Binary.BinaryHandler.StreamWriterAsync(controller.pipeServer, message);
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
                try
                {
                    controller.pipeServer.Disconnect();
                }
                catch
                {
                    // Exception caused by disconecction on client side.
                }
            }

            // Log about transmission finish.
            Console.WriteLine("TRANSMISSION FINISHED AT {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
        }
    }
}
