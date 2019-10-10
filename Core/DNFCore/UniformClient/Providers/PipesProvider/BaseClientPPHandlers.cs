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

using PipesProvider.Security.Encryption;
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
            byte[] receivedData = null;
            try
            {
                Console.WriteLine("{0}/{1}: WAITING FOR MESSAGE",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);

                // Avoid an error caused to disconection of client.
                try
                {
                    receivedData = await UniformDataOperator.Binary.IO.StreamHandler.StreamReaderAsync(lineProcessor.pipeClient);
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
            // Catch the Exception that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                // Log
                if (receivedData == null || receivedData.Length == 0)
                {
                    Console.WriteLine("DNS HANDLER ERROR ({1}): {0}", e.Message, lineProcessor.pipeClient.GetHashCode());
                }
            }
            #endregion

            // Log
            if (receivedData == null || receivedData.Length == 0)
            {
                Console.WriteLine("{0}/{1}: RECEIVED MESSAGE IS EMPTY.",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);
            }
            else
            {
                // Log state.
                Console.WriteLine("{0}/{1}: MESSAGE RECIVED",
                        lineProcessor.ServerName, lineProcessor.ServerPipeName);

                // Decode to Query.
                UniformQueries.Query receviedQuery;
                try
                {
                    receviedQuery = UniformDataOperator.Binary.BinaryHandler.FromByteArray<UniformQueries.Query>(receivedData);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("DNS HANDLER ERROR 401({1}): QUERY DAMAGED : {0}", ex.Message, lineProcessor.pipeClient.GetHashCode());

                    // Stop processing merker to pass async block.
                    lineProcessor.Processing = false;

                    // Close processor case this line already deprecated on the server side as single time task.
                    lineProcessor.Close();
                    return;
                }

                // Decrypt if required.
                await EnctyptionOperatorsHandler.TryToDecryptAsync(
                    receviedQuery, 
                    EnctyptionOperatorsHandler.AsymmetricKey,
                    TerminationTokenSource.Token);

                #region Processing message
                // Try to call answer handler.
                string tableGUID = lineProcessor.ServerName + "\\" + lineProcessor.ServerPipeName;

                // Look for delegate in table.
                if (DuplexBackwardCallbacks[tableGUID] is
                    Action<TransmissionLine, UniformQueries.Query> registredCallback)
                {
                    if (registredCallback != null)
                    {
                        // Invoke delegate if found and has dubscribers.
                        registredCallback.Invoke(lineProcessor, receviedQuery);

                        // Drop reference.
                        DuplexBackwardCallbacks.Remove(tableGUID);

                        // Inform subscribers about answer receiving.
                        eDuplexBackwardCallbacksReceived?.Invoke(tableGUID);
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
            if (!(sharedObject is PipesProvider.Client.TransmissionLine line))
            {
                Console.WriteLine("TRANSMISSION ERROR (UQPP0): INCORRECT TRANSFERED DATA TYPE. PERMITED ONLY \"LineProcessor\"");
                return;
            }

            // Configurate line.
            if(!await ConfigurateTransmissionLine(line))
            {
                return;
            }

            // Open stream writer.
            try
            {
                #region Answer waiting
                // Wait for answer if reqired.
                if (line.LastQuery.Data.WaitForAnswer)
                {
                    // Trying to compute backward domain.
                    if (UniformQueries.QueryPart.TryGetBackwardDomain(line.LastQuery.Data, out string domain))
                    {
                        domain = line.ServerName + "\\" + domain;
                        // Subscribe on global event.
                        eDuplexBackwardCallbacksReceived += AnswerReceivedCallback;

                        // Callback that will be called wehen server will receive answer on current request.
                        void AnswerReceivedCallback(string guid)
                        {
                            if(guid.Equals(domain))
                            {
                                // Unsubscribe from events.
                                eDuplexBackwardCallbacksReceived -= AnswerReceivedCallback;

                                // Unclock queue.
                                line.LastQuery.Data.WaitForAnswer = false;

                                // Log to console.
                                Console.WriteLine("{0}/{1}: Answer received. Queue unlocked.",
                                    line.ServerName, line.ServerPipeName);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("TRANSMISSION ERROR (UQPP3): Unable to build backward domain. " +
                            "Operation waiting not possible. Query terminated.");
                        return;
                    }
                }

                #endregion

                // Start writing data to stream.
                await UniformDataOperator.Binary.IO.StreamHandler.StreamWriterAsync(
                    line.pipeClient, 
                    line.LastQuery.Data);

                // Log that data tansmited.
                Console.WriteLine("TRANSMITED: {0}", line.LastQuery);    
                
                // Waiting for answer fro server.
                while(line.LastQuery.Data.WaitForAnswer)
                {
                    Thread.Sleep(5);
                }
            }
            // Catch the Exception that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.WriteLine("DNS HANDLER ERROR ({1}): {0}", e.Message, line.pipeClient.GetHashCode());

                // Retry transmission.
                if (line.LastQuery.Attempts < 10)
                {
                    // Add to queue.
                    line.EnqueueQuery(line.LastQuery);

                    // Add attempt.
                    line++;
                }
                else
                {
                    // If transmission attempts over the max count.
                }
            }

            // Unlock loop.
            line.Processing = false;
        }

        /// <summary>
        /// Validating and fixing configuration andd params of transmission line.
        /// </summary>
        /// <param name="line">Target line.</param>
        /// <returns>Result of configurating. False - failed.</returns>
        public static async Task<bool> ConfigurateTransmissionLine(TransmissionLine line)
        {
            if (line.RoutingInstruction != null) // Routing instruction applied.
            {
                // Is partial autorized instruction.
                if (line.RoutingInstruction is PartialAuthorizedInstruction pai)
                {
                    #region Token validation
                    // If token not exist, or emoty, or expired.
                    if (line.LastQuery.Data.TryGetParamValue("token", out UniformQueries.QueryPart token))
                    {
                        // If token value is emoty.
                        if (string.IsNullOrEmpty(token.PropertyValueString))
                        {
                            if (await TokenValidation(pai))
                            {
                                // Apply received token.
                                line.LastQuery.Data.SetParam(new UniformQueries.QueryPart("token", pai.GuestToken));
                            }
                            else return false;
                        }
                    }
                    else
                    {
                        // Validate token
                        if (await TokenValidation(pai))
                        {
                            // Add tokent to query,
                            line.LastQuery.Data.ListedContent?.Add(new UniformQueries.QueryPart("token", pai.GuestToken));
                        }
                        else return false;
                    }

                    #region Methods
                    // Trying to porovide valid guest token.
                    // Returns result of validation. False - token invalid.
                    async Task<bool> TokenValidation(PartialAuthorizedInstruction instruction)
                    {
                        // If guest token is not found or expired.
                        if (pai.GuestToken == null ||
                            UniformQueries.Tokens.IsExpired(instruction.GuestToken, instruction.GuestTokenHandler.ExpiryTime))
                        {
                            // Wait for token.
                            if (!await pai.TryToGetGuestTokenAsync(TerminationTokenSource.Token))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    #endregion
                    #endregion

                    #region Encriprion operators validation
                    if(line.LastQuery.Data.IsEncrypted && // Is query require encryption? (Query can't be encrypted if that query must request key for encryption.)
                       line.RoutingInstruction != null && // Is routing instruction is valid and allow encryption?
                       line.RoutingInstruction.encryption)
                    {
                        // Check asymmetric operator.
                        if(pai.AsymmetricEncryptionOperator == null)
                        {
                            Console.WriteLine("Encryption operator not configurated. Operation declined.");
                            return false; 
                        }

                        #region Receiving public key from intruction's target server
                        // Check if operator is valid.
                        if (!pai.AsymmetricEncryptionOperator.IsValid)
                        {
                            try
                            {
                                // Start async key exchanging.
                                var keysExchangingOperator = RequestRSAEncryptionKeyAsync(pai);

                                try
                                {
                                    if (!keysExchangingOperator.IsCompleted)
                                    {
                                        keysExchangingOperator.Start();
                                    }
                                }
                                catch { };

                                // Drop current query as invalid cause encryption operator still in processing.
                                line.Processing = false;
                                return false;
                            }
                            catch (Exception ex)
                            {
                                // Unlock loop.
                                line.Processing = false;
                                Console.WriteLine("SECRET KEY ERROR (bcpph1): " + ex.Message);
                                return false;
                            }
                        }
                        #endregion

                        #region Adding public key to backward encryption if not added yet
                        if(!line.LastQuery.Data.QueryParamExist("pk"))
                        {
                            line.LastQuery.Data.SetParam(new UniformQueries.QueryPart(
                                "pk",
                                EnctyptionOperatorsHandler.AsymmetricKey.EncryptionKey));
                        }
                        #endregion

                        // Encrypt query by public key of target server.
                        return await EnctyptionOperatorsHandler.TryToEncryptAsync(
                            line.LastQuery.Data, 
                            line.LastQuery.Data.EncryptionMeta.contentEncytpionOperatorCode,
                            line.RoutingInstruction.AsymmetricEncryptionOperator,
                            TerminationTokenSource.Token);
                    }
                    #endregion
                }
            }

            return true;
        }
    }
}
