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
        /// <summary>
        /// Routing instruction that contains settings to relay establishment and maintenance.
        /// </summary>
        public RelayInstruction relayInstruction;

        // Init default constructor.
        public RelayServer() : base()
        {

        }

        /// <summary>
        /// Establish server suitable provided instruction that would retranslate broadcasting from target server.
        /// </summary>
        /// <param name="relayInstruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static UniformServer.BaseServer EstablishBroadcastingRelayServer(RelayInstruction isntruction)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Establishing server that would recive client's server and forwarding it to target servers by using routing table.
        /// </summary>
        /// <param name="relayInstruction">Instruction that contain relay params.</param>
        /// <returns>Established server.</returns>
        public static UniformServer.BaseServer EstablishDuplexRelayServer(RelayInstruction relayInstruction)
        {
            if(relayInstruction == null)
            {
                throw new NullReferenceException("Routing table can't be null");
            }

            // Instiniate server.
            RelayServer serverBufer = new RelayServer();

            // Set fields.
            serverBufer.pipeName = relayInstruction.entryPipeName;

            // Starting server loop.
            serverBufer.StartServerThread(
                relayInstruction.entryPipeName + " #" + Guid.NewGuid(), 
                serverBufer,
                ThreadingServerLoop_Relay);

            return serverBufer;
        }

        /// <summary>
        ///  Start the server loop that will condtol relay query handler.
        /// </summary>
        protected static void ThreadingServerLoop_Relay(object server)
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
                QueryHandler_Relay,
                ((RelayServer)server).pipeName,
                ((RelayServer)server).securityLevel);
            #endregion
        }


        /// <summary>
        /// Redirect recived query from current server to other.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="query"></param>
        public static void QueryHandler_Relay(BaseServerTransmissionController _, string query)
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
    }
}
