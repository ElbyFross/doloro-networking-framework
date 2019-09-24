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
        /// Provide valid public key for target server encryption.
        /// Auto update key if was expired.
        /// </summary>
        /// <param name="instruction">Routing instruction that contains transmission descriptor.</param>
        /// <returns>Started async task.</returns>
        public static async Task GetValidSecretKeysViaPPAsync(Instruction instruction)
        {
            // Try to cast instruction like a partial authorized (that has guest authorized token).
            if (instruction is PartialAuthorizedInstruction pai)
            {
                if (string.IsNullOrEmpty(pai.GuestToken))
                {
                    // Request guest token and only after that request public key.
                    await pai.TryToGetGuestTokenAsync(RequestEncryptionKeys, TerminationTokenSource.Token);
                }
                else
                {
                    // Request pai if 
                    if (!pai.IsValid)
                    {
                        RequestEncryptionKeys(pai);
                    }
                }
            }
            else
            {
                throw new InvalidCastException("Instruction must be inheirted from PartialAuthorizedInstruction");
            }
        }

        /// <summary>
        /// Requsting RSA and AES encryption keys from server.
        /// </summary>
        /// <param name="pai">Instruction that would be used for routing to target server.</param>
        private static void RequestEncryptionKeys(PartialAuthorizedInstruction pai)
        {
            // Validate key.
            if (pai.IsValid)
            {
                return;
            }

            // Create base part of query for reciving of public RSA key.
            UniformQueries.Query query = new UniformQueries.Query(
                new UniformQueries.QueryPart("token", pai.GuestToken),
                new UniformQueries.QueryPart("GET"),
                new UniformQueries.QueryPart("PUBLICKEY"),
                new UniformQueries.QueryPart("guid", pai.GetHashCode().ToString()));
            
            // Request public key from server.
            EnqueueDuplexQueryViaPP(
                pai.routingIP,
                pai.pipeName,
                query,
                // Create callback delegate that will set recived value to routing table.
                delegate (TransmissionLine answerLine, UniformQueries.Query answer)
                {
                    // Log about success.
                    //Console.WriteLine("{0}/{1}: PUBLIC KEY RECIVED",
                    //    instruction.routingIP, instruction.pipeName);

                    // Try to apply recived answer.
                    if(pai.RSAEncryptionOperator.UpdateWithQuery(answer))
                    {
                        // TODO Request AES keys exchange.
                    }

                    // Log about update
                    Console.WriteLine("{0}/{1}: RSA PUBLIC KEY UPDATED",
                         pai.routingIP, pai.pipeName);
                });
        }
    }
}
