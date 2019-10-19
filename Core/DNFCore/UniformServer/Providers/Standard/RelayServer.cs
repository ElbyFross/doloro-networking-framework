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

using PipesProvider.Client;
using PipesProvider.Networking.Routing;
using PipesProvider.Server.TransmissionControllers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniformServer.Standard
{
    /// <summary>
    /// Server that provide API for relaying of transmission.
    /// </summary>
    public class RelayServer : BaseServer
    {
        /// <summary>
        /// Instiniate relay server.
        /// </summary>
        public RelayServer() : base()
        {

        }

        /// <summary>
        /// Auto detect behavior of instruction and start relative relay server.
        /// </summary>
        /// <param name="instruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static RelayServer EstablishAutoRelayServer(RelayInstruction instruction)
        {
            switch(instruction.behavior)
            {
                case RelayInstruction.RelayBehavior.Duplex:
                    return EstablishDuplexRelayServer(instruction);

                case RelayInstruction.RelayBehavior.Broadcasting:
                    return EstablishBroadcastingRelayServer(instruction);

                default:
                    throw new InvalidCastException("Invalid behavior type. Relay server not started.");
            }
        }

        #region Broadcasting retranslator
        /// <summary>
        /// Establish server suitable provided instruction that would retranslate broadcasting from target server.
        /// </summary>
        /// <param name="instruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static RelayServer EstablishBroadcastingRelayServer(RelayInstruction instruction)
        {
            // Check instruction.
            if (instruction == null)
            {
                throw new NullReferenceException("Routing instruction can't be null");
            }

            // Instiniate server.
            RelayServer serverBufer = new RelayServer
            {
                // Set fields.
                pipeName = instruction.entryPipeName
            };

            // Starting server loop.
            serverBufer.StartServerThread(
                instruction.entryPipeName + " #" + Guid.NewGuid(),
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
            Console.WriteLine("BROADCASTING RELAY THREAD STARTED: {0}", Thread.CurrentThread.Name);

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
        /// <param name="controller">Controller that manage curernt transmission.</param>
        public static byte[] QueryHandler_BroadcastingRelay(BroadcastingServerTransmissionController controller)
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

                return  UniformDataOperator.Binary.BinaryHandler.ToByteArray("Error 404: Routing server not found. Con'tact administrator.");
            }

            // Markers for managing thread.
            bool relayedMessageRecieved = false;
            byte[] relayedData = null;

            // Log
            Console.WriteLine("Requesting broadcasting: " + relayInstruction.routingIP + "/" + relayInstruction.pipeName);

            // Requiest message from relaying broadcasting server.
            UniformClient.BaseClient.ReceiveAnonymousBroadcastMessage(
                relayInstruction.routingIP,
                relayInstruction.pipeName,
                delegate (TransmissionLine lint, UniformQueries.Query query)
                {
                    // Conver message to string.
                    relayedData = UniformDataOperator.Binary.BinaryHandler.ToByteArray(query);

                    // Unlock thread.
                    relayedMessageRecieved = true;
                });

            // Wait until broadcasting message.
            while (!relayedMessageRecieved)
            {
                Thread.Sleep(15);
            }

            // Return recived message.
            return relayedData;
        }
        #endregion

        #region Duplex query retranslator
        /// <summary>
        /// Establishing server that would recive client's server and forwarding it to target servers by using routing table.
        /// </summary>
        /// <param name="instruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static RelayServer EstablishDuplexRelayServer(RelayInstruction instruction)
        {
            // Check instruction.
            if(instruction == null)
            {
                throw new NullReferenceException("Routing instruction can't be null");
            }

            // Instiniate server.
            RelayServer serverBufer = new RelayServer
            {
                // Set fields.
                pipeName = instruction.entryPipeName
            };

            // Starting server loop.
            serverBufer.StartServerThread(
                instruction.entryPipeName + " #" + Guid.NewGuid(), 
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
            Console.WriteLine("DUPLEX RELAY THREAD STARTED: {0}", Thread.CurrentThread.Name);

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
        /// <param name="tc">Server's transmission controller.</param>
        /// <param name="query">Query received from client.</param>
        public static void QueryHandler_DuplexRelay(BaseServerTransmissionController tc, UniformQueries.Query query)
        {
            bool decryptionComplete = false;
            bool decryptionResult = false;

            Task decryptionOperation = new Task(async delegate ()
            {
                // Try to encrypt receved message.
                decryptionResult = await PipesProvider.Security.Encryption.EnctyptionOperatorsHandler.TryToDecryptAsync(
                    query,
                    PipesProvider.Security.Encryption.EnctyptionOperatorsHandler.AsymmetricKey);

                decryptionComplete = true; 
            });
            decryptionOperation.Start();

            // Whait for decription completing.
            while (!decryptionComplete)
            {
                Thread.Sleep(5);
            }

            // Loging error to client.
            if(!decryptionResult)
            {
                // Try to get answer in string format.
                SendAnswerViaPP("DUPLEX RELEAY ERROR: Data corrupted. Decryption not possible. Relay terminated.", query);
                return;
            }

            // Detect routing target.
            bool relayTargetFound = UniformClient.BaseClient.routingTable.TryGetRoutingInstruction(query, out Instruction instruction);

            // If instruction not found.
            if (!relayTargetFound)
            {
                // If reley target not found then server will mean that query requested to itself.
                PipesProvider.Handlers.Queries.ProcessingAsync(tc, query);

                //// Log
                //Console.WriteLine("RELAY TARGET NOT FOUND: {q}", query);

                //// DO BACKWARED ERROR INFORMATION.
                //SendAnswer("error=404", UniformQueries.API.DetectQueryParts(query));

                // Drop continue computing.
                return;
            }

            // Open connection.
            TransmissionLine tl = UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                instruction.routingIP,
                instruction.pipeName,
                query,
                // Delegate that will called when relayed server send answer.
                // Redirect this answer to client.
                delegate (TransmissionLine answerTL, UniformQueries.Query receivedData)
                {
                    // Try to get answer in string format.
                    SendAnswerViaPP(receivedData, query);
                    return;
                });
        }
        #endregion
    }
}
