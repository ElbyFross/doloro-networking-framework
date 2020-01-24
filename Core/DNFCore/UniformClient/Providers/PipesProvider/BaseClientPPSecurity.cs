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
using System.Collections.Generic;
using System.Threading.Tasks;
using PipesProvider.Networking.Routing;
using PipesProvider.Client;

namespace UniformClient
{
    /// <summary>
    /// Part of class that provides secure methods and fields.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Requsting a public encryption key from a server definded into the instruction.
        /// </summary>
        /// <param name="pai">An instruction that would be used for routing to target server.</param>
        /// <returns>A started async task that returns result of keys exchanging process.</returns>
        private static async Task<bool> RequestPublicEncryptionKeyAsync(PartialAuthorizedInstruction pai)
        {
            // TODO not work.

            // Create base part of query for reciving of public RSA key.
            UniformQueries.Query query = null;

            try
            {
                query = new UniformQueries.Query(
                    null,
                    new UniformQueries.QueryPart("token", pai.GuestToken),
                    new UniformQueries.QueryPart("get"),
                    new UniformQueries.QueryPart("publickey"),
                    new UniformQueries.QueryPart("guid", pai.GetHashCode().ToString()))
                {
                    WaitForAnswer = true // Request waiting for answer before computing of the next query in queue.
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine("Encryption failed (rpeka10): " + ex.Message);
                return false;
            }

            // Getting line to prevent conflict.
            TransmissionLine line = OpenOutTransmissionLineViaPP(pai.routingIP, pai.pipeName);

            // Insert query to processing with termination of current query.
            line.InsertQuery(query, true);

            bool result = false;
            bool completed = false;

            // Open backward chanel to recive answer from server.
            ReceiveDelayedAnswerViaPP(line, query, 
                // Create callback delegate that will set recived value to routing table.
                delegate (TransmissionLine answerLine, UniformQueries.Query answer)
                {
                    // Try to apply recived answer.
                    result = pai.AsymmetricEncryptionOperator.UpdateWithQuery(answer);

                    // Log about update
                    Console.WriteLine("{0}/{1}: \"{2}\" public key updating status {3}",
                         pai.routingIP, pai.pipeName, 

                         PipesProvider.Security.Encryption.EnctyptionOperatorsHandler
                         .GetOperatorCode(pai.AsymmetricEncryptionOperator),

                         result);

                    // Finalize operation.
                    completed = true;
                });

            // Wait for result.
            while(!completed)
            {
                await Task.Delay(20, ClientAppConfigurator.TerminationTokenSource.Token);
            }

            return result;
        }
    }
}
