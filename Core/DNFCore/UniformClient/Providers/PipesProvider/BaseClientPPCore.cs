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
    /// Part of class that provide core methods and fields.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Allow to start thread but previous return turn to current thread.
        /// Allow to use single line queries.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="guid"></param>
        /// <param name="trnsLine"></param>
        protected static async void StartPPClientThreadAsync(BaseClient client, string guid, TransmissionLine trnsLine)
        {
            await Task.Run(() => {
                client.StartClientThread(
                guid,
                trnsLine,
                TransmissionLine.ThreadLoop);
            });
        }
        
        /// <summary>
        /// Provide valid public key for target server encryption.
        /// Auto update key if was expired.
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        public static RSAParameters GetValidPublicKeyViaPP(Instruction instruction)
        {
            // Try to cast instruction like a partial authorized (that has guest authorized token).
            if (instruction is PartialAuthorizedInstruction partialAuthorizedInstruction)
            {
                // Validate key.
                if (!instruction.IsValid)
                {
                    // Create base part of query for reciving of public RSA key.
                    string query = string.Format("token={1}{0}q=GET{0}sq=PUBLICKEY",
                        UniformQueries.API.SPLITTING_SYMBOL, partialAuthorizedInstruction.GuestToken);

                    // Request public key from server.
                    EnqueueDuplexQueryViaPP(
                        instruction.routingIP,
                        instruction.pipeName,
                        // Add guid base on instruction hash to this query.
                        query + UniformQueries.API.SPLITTING_SYMBOL + "guid=" + instruction.GetHashCode(),
                        // Create callback delegate that will set recived value to routing table.
                        delegate (TransmissionLine answerLine, object answer)
                        {
                        // Log about success.
                        //Console.WriteLine("{0}/{1}: PUBLIC KEY RECIVED",
                        //    instruction.routingIP, instruction.pipeName);

                        // Try to apply recived answer.
                        instruction.TryUpdatePublicKey(answer);
                        });
                }

                // Return current public key.
                return instruction.PublicKey;
            }
            else
            {
                throw new InvalidCastException("Instruction must be inheirted from PartialAuthorizedInstruction");
            }
        }
    }
}
