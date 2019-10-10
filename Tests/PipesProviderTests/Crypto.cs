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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using UniformQueries;

namespace Tests
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class Crypto
    {
        /// <summary>
        /// Checking 2 steps sequrity of chanel.
        /// </summary>
        [TestMethod]
        public void SecretKeyExchanging()
        {
            // Start server that would manage that data.
            ACTests.Helpers.Networking.StartPublicServer(3);

            // Start broadcasting server that would share guest tokens.
            UniformServer.Standard.BroadcastingServer.StartBroadcastingViaPP(
                "guests",
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);


            // Build routing instruction.
            PipesProvider.Networking.Routing.Instruction pai = new PipesProvider.Networking.Routing.PartialAuthorizedInstruction()
            {
                pipeName = ACTests.Helpers.Networking.DefaultQueriesPipeName,
                routingIP = "localhost",
                encryption = true,
                title = "TestPipe",
                guestChanel = "guests"
            };

            // Create the query that would simulate logon.
            // TODO Add encyption.
            Query query = new Query(
                new Query.EncryptionInfo()
                { contentEncytpionOperatorCode = "aes" },

                new QueryPart("token", ((PipesProvider.Networking.Routing.PartialAuthorizedInstruction)pai).GuestToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user"),
                new QueryPart("logon"),

                new QueryPart("login", "user"),
                new QueryPart("password", "invalidPassword"),
                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString())
            );

            // Marker that avoid finishing of the test until receiving result.
            bool operationCompete = false;

            // Start reciving clent line.
            UniformClient.BaseClient.EnqueueDuplexQueryViaPP(
                "localhost", ACTests.Helpers.Networking.DefaultQueriesPipeName,
                query,
                // Handler that would recive ther ver answer.
                (PipesProvider.Client.TransmissionLine line, Query answer) =>
                {
                    // Is operation success?
                    if (answer.First.PropertyValueString.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        // Log error.
                        Assert.IsTrue(true);
                        operationCompete = true;
                    }
                    else
                    {
                        // Log error.
                        Assert.Fail("Unexisted user found on server.\nAnswer:" + answer.First.PropertyValueString);
                        operationCompete = true;
                    }
                }).SetInstructionAsKey(ref pai);

            //Thread.Sleep(1500);
            //return;

            // Wait until operation would complete.
            while (!operationCompete)
            {
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// Chcking AES encryption\decryption process.
        /// </summary>
        [TestMethod]
        public void AESEncryption()
        {
            var roundtrip = "This is the data I am encrypting.  There are many like it but this is my encryption.";

            // Create encryption operator.
            var eOperator = new PipesProvider.Security.Encryption.Operators.AESEncryptionOperator();

            // Encrypt data.
            byte[] encryptedData = eOperator.Encrypt(Encoding.UTF8.GetBytes(roundtrip));

            // Decrypt data.
            byte[] decryptedData = eOperator.Decrypt(encryptedData);

            // Encode binary data to string.
            string decryptedMessage = Encoding.UTF8.GetString(decryptedData).TrimEnd('\0');

            Assert.IsTrue(roundtrip == decryptedMessage);
        }
    }
}
