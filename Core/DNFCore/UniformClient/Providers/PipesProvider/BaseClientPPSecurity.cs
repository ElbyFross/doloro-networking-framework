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
        /// Requesting secrete key.
        /// </summary>
        /// <param name="encryptionProviderKey">Code of target encryption operator.</param>
        /// <param name="pai">Routing instruction to target server.</param>
        /// <returns></returns>
        [Obsolete]
        public static async Task<bool> RequestSecretKeyViaPPAsync(string encryptionProviderKey, PartialAuthorizedInstruction pai)
        {
            // Validate guest token.
            if(string.IsNullOrEmpty(pai.GuestToken))
            {
                // Waiting for guest token.
                if(!await pai.TryToGetGuestTokenAsync(TerminationTokenSource.Token))
                {
                    Console.WriteLine("{0}/{1}: Guest token receiving failed. Keys enchange terminated.");
                    return false;
                }
            }

            encryptionProviderKey = encryptionProviderKey.ToLower();

            switch (encryptionProviderKey)
            {
                case "rsa": return await RequestPublicEncryptionKeyAsync(pai);
                //case "aes": return await RequestAESEncryptionKeyAsync(pai);
                default: throw new NotSupportedException("\"" + encryptionProviderKey + "\" IEncryptionOperator not exist in that instruction.");
            }

        }

        /// <summary>
        /// Requsting a public encryption key from a server definded into the instruction.
        /// </summary>
        /// <param name="pai">An instruction that would be used for routing to target server.</param>
        /// <returns>A started async task that returns result of keys exchanging process.</returns>
        private static async Task<bool> RequestPublicEncryptionKeyAsync(PartialAuthorizedInstruction pai)
        {
            // Create base part of query for reciving of public RSA key.
            UniformQueries.Query query = new UniformQueries.Query(
                new UniformQueries.QueryPart("token", pai.GuestToken),
                new UniformQueries.QueryPart("get"),
                new UniformQueries.QueryPart("publickey"),
                new UniformQueries.QueryPart("guid", pai.GetHashCode().ToString()))
            {
                WaitForAnswer = true // Request waitign for answer before computing of the next query in queue.
            };

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
                    Console.WriteLine("{0}/{1}: RSA PUBLIC KEY UPDATED",
                         pai.routingIP, pai.pipeName);

                    // Finalize operation.
                    completed = true;
                });

            // Wait for result.
            while(!completed)
            {
                await Task.Delay(20, TerminationTokenSource.Token);
            }

            return result;
        }

        /*
        /// <summary>
        /// Requsting AES encryption key from server.
        /// </summary>
        /// <param name="pai">Instruction that would be used for routing to target server.</param>
        private static async Task<bool> RequestAESEncryptionKeyAsync(PartialAuthorizedInstruction pai)
        {
            // Check if RSA encryptor alredy received and valid.
            if(!pai.AsymmetricEncryptionOperator.IsValid)
            {
                // Requestung new RSA key.
                if(!await RequestRSAEncryptionKeyAsync(pai))
                {
                    return false;
                }
            }

            // Create base part of query for reciving of public RSA key.
            UniformQueries.Query query = new UniformQueries.Query(
                new UniformQueries.QueryPart("token", pai.GuestToken),
                new UniformQueries.QueryPart("get"),
                new UniformQueries.QueryPart("skey"),
                new UniformQueries.QueryPart("guid", pai.GetHashCode().ToString()));

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
                    result = pai.AESEncryptionOperator.UpdateWithQuery(answer);

                    // Log about update
                    Console.WriteLine("{0}/{1}: AES KEY UPDATED",
                         pai.routingIP, pai.pipeName);

                    // Finalize operation.
                    completed = true;
                });

            // Wait for result.
            while (!completed)
            {
                await Task.Delay(20, TerminationTokenSource.Token);
            }

            return result;
        }
        */
    }
}
