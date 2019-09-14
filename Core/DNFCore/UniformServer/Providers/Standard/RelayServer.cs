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
using System.Threading;
using PipesProvider.Server.TransmissionControllers;
using PipesProvider.Networking.Routing;
using PipesProvider.Client;

namespace UniformServer.Standard
{
    /// <summary>
    /// Server that provide API for relaying of transmission.
    /// </summary>
    public class RelayServer : BaseServer
    {
        // Init default constructor.
        public RelayServer() : base()
        {

        }

        #region Broadcasting retranslator
        /// <summary>
        /// Establish server suitable provided instruction that would retranslate broadcasting from target server.
        /// </summary>
        /// <param name="relayInstruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static RelayServer EstablishBroadcastingRelayServer(RelayInstruction isntruction)
        {
            // Check instruction.
            if (isntruction == null)
            {
                throw new NullReferenceException("Routing instruction can't be null");
            }

            // Instiniate server.
            RelayServer serverBufer = new RelayServer
            {
                // Set fields.
                pipeName = isntruction.entryPipeName
            };

            // Starting server loop.
            serverBufer.StartServerThread(
                isntruction.entryPipeName + " #" + Guid.NewGuid(),
                serverBufer,
                ThreadingServerLoop_BroadcastingRelay);

            return serverBufer;
        }

        /// <summary>
        /// Starting the server loop that will control relay query handler.
        /// </summary>
        protected static void ThreadingServerLoop_BroadcastingRelay(object server)
        {
            #region Init
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Name of pipe server that will established.
            // Access to this pipe by clients will be available by this name.
            string serverName = ((RelayServer)server).thread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            BroadcastingServerTransmissionController.ServerLoop(
                serverName,
                ((RelayServer)server).pipeName,
                ((RelayServer)server).securityLevel,
                QueryHandler_BroadcastingRelay);
            #endregion
        }

        /// <summary>
        /// Redirect recived query from current server to other.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="query"></param>
        public static string QueryHandler_BroadcastingRelay(BroadcastingServerTransmissionController controller)
        {
            // Trying to detect relay instruction.
            if (!RelayInstruction.TryToDetectTarget(
               UniformClient.BaseClient.routingTable.intructions,
               controller.pipeName,
               out RelayInstruction relayInstruction))
            {
                Console.WriteLine(
                    "Relay instruction for \""
                    + controller.pipeName +
                    "\" not found. Add instuction to \"BaseClient.routingTable.intructions\" collection.");

                return "Error 404: Routing server not found. Con'tact administrator.";
            }

            // Markers for managing thread.
            bool relayedMessageRecieved = false;
            string relayedMessage = null;

            // Requiest message from relaying broadcasting server.
            UniformClient.BaseClient.ReceiveAnonymousBroadcastMessage(
                relayInstruction.routingIP,
                relayInstruction.pipeName,
                delegate (TransmissionLine lint, object message)
                {
                    // Conver message to string.
                    relayedMessage = message as string;

                    // Unlock thread.
                    relayedMessageRecieved = true;
                });

            // Wait until broadcasting message.
            while (!relayedMessageRecieved)
            {
                Thread.Sleep(15);
            }

            // Return recived message.
            return relayedMessage;
        }
        #endregion

        #region Duplex query retranslator
        /// <summary>
        /// Establishing server that would recive client's server and forwarding it to target servers by using routing table.
        /// </summary>
        /// <param name="isntruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static RelayServer EstablishDuplexRelayServer(RelayInstruction isntruction)
        {
            // Check instruction.
            if(isntruction == null)
            {
                throw new NullReferenceException("Routing instruction can't be null");
            }

            // Instiniate server.
            RelayServer serverBufer = new RelayServer
            {
                // Set fields.
                pipeName = isntruction.entryPipeName
            };

            // Starting server loop.
            serverBufer.StartServerThread(
                isntruction.entryPipeName + " #" + Guid.NewGuid(), 
                serverBufer,
                ThreadingServerLoop_DuplexRelay);

            return serverBufer;
        }

        /// <summary>
        /// Starting the server loop that will control relay query handler.
        /// </summary>
        protected static void ThreadingServerLoop_DuplexRelay(object server)
        {
            #region Init
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Name of pipe server that will established.
            // Access to this pipe by clients will be available by this name.
            string serverName = ((RelayServer)server).thread.Name;
            #endregion

            #region Server establishing
            // Start server loop.
            PipesProvider.Server.TransmissionControllers.ClientToServerTransmissionController.ServerLoop(
                serverName,
                QueryHandler_DuplexRelay,
                ((RelayServer)server).pipeName,
                ((RelayServer)server).securityLevel);
            #endregion
        }
        
        /// <summary>
        /// Redirect recived query from current server to other.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="query"></param>
        public static void QueryHandler_DuplexRelay(BaseServerTransmissionController _, string query)
        {
            // Try to decrypt.
            query = PipesProvider.Security.Crypto.DecryptString(query);

            // Detect routing target.
            bool relayTargetFound = UniformClient.BaseClient.routingTable.TryGetRoutingInstruction(query, out Instruction instruction);

            // If instruction not found.
            if (!relayTargetFound)
            {
                // If reley target not found then server will mean that query requested to itself.
                PipesProvider.Handlers.Query.ProcessingAsync(_, query);

                //// Log
                //Console.WriteLine("RELAY TARGET NOT FOUND: {q}", query);

                //// DO BACKWARED ERROR INFORMATION.
                //SendAnswer("error=404", UniformQueries.API.DetectQueryParts(query));

                // Drop continue computing.
                return;
            }

            // If requested encryption.
            if (instruction.RSAEncryption)
            {
                // Check if instruction key is valid.
                // If key expired or invalid then will be requested new.
                if (!instruction.IsValid)
                {
                    // Request new key.
                    UniformClient.BaseClient.GetValidPublicKeyViaPP(instruction);

                    // Log.
                    Console.WriteLine("WAITING FOR PUBLIC RSA KEY FROM {0}/{1}", instruction.routingIP, instruction.pipeName);

                    // Wait until validation time.
                    // Operation will work in another threads, so we just need to take a time.
                    while (!instruction.IsValid)
                    {
                        Thread.Sleep(15);
                    }

                    // Log.
                    Console.WriteLine("PUBLIC RSA KEY FROM {0}/{1} RECIVED", instruction.routingIP, instruction.pipeName);
                }

                // Encrypt query by public key of target server.
                query = PipesProvider.Security.Crypto.EncryptString(query, instruction.PublicKey);
            }

            // Open connection.
            TransmissionLine tl = UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                instruction.routingIP,
                instruction.pipeName,
                query,
                // Delegate that will called when relayed server send answer.
                // Redirect this answer to client.
                delegate (PipesProvider.Client.TransmissionLine answerTL, object answer)
                {
                    // Try to get answer in string format.
                    string answerAsString = answer as string;
                    if (!string.IsNullOrEmpty(answerAsString))
                    {
                        UniformServer.BaseServer.SendAnswerViaPP(answerAsString, UniformQueries.API.DetectQueryParts(query));
                        return;
                    }

                    // Try to get answer as byte array.
                    if (answer is byte[] answerAsByteArray)
                    {
                        // TODO Send answer as byte array.
                        throw new NotImplementedException();
                    }
                });
        }
        #endregion
    }
}
