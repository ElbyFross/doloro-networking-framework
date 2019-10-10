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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthorityController.Queries;
using UniformServer;

namespace ACTests.Tests
{
    /// <summary>
    /// Provide usit test for query processors.
    /// </summary>
    [TestClass]
    public class QueryProcessors
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Stop all started servers.
            PipesProvider.Server.ServerAPI.StopAllServers();
        }

        /// <summary>
        /// Logon processor test.
        /// </summary>
        [TestMethod]
        public void Logon()
        {
            bool operationCompleted = false;
            bool operationResult = false;
            string recivedMessage = null;

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create users for test.
                Helpers.Users.SetBaseUsersPool();

                // Start server that would manage users data.
                Helpers.Networking.StartPublicServer();

                // Create new processor that provide logon operation.
                USER_LOGON.LogonProcessor processor = new USER_LOGON.LogonProcessor();

                // Sign up on callback that would be called when logon operation would be passed.
                processor.ProcessingFinished += delegate (
                    UniformQueries.Executable.QueryProcessor _,
                    bool result,
                    object message)
                {
                    // Buferize data.
                    operationResult = result;
                    recivedMessage = message as string;

                    Console.WriteLine("LOGON FINISHED | TOKEN:" + recivedMessage);

                    // Continue thread.
                    operationCompleted = true;
                };

                // Request logon.
                processor.TryToLogonAsync(
                    Helpers.Users.user_User.tokens[0], // Use valid token.
                    "user",
                    "password",
                    "localhost",
                     Helpers.Networking.DefaultQueriesPipeName);


                //Thread.Sleep(2000);
                //return;

                // Wait until logon would compleated.
                while (!operationCompleted)
                {
                    Thread.Sleep(5);
                }
                                
                // Assert result based on received answer.
                Assert.IsTrue(operationResult, recivedMessage);
            }
        }

        /// <summary>
        /// GetGuestToken processor test.
        /// </summary>
        [TestMethod]
        public void GuestToken()
        {
            // Start broadcasting server that would share guest tokens.
            UniformServer.Standard.BroadcastingServer.StartBroadcastingViaPP(
                Helpers.Networking.DefaultGuestPipeName,
                PipesProvider.Security.SecurityLevel.Anonymous,
                AuthorityController.API.Tokens.AuthorizeNewGuestToken,
                1);

            Thread.Yield();

            // Drop with fail if server not found.
            if (!PipesProvider.NativeMethods.DoesNamedPipeExist(
                "localhost",
                Helpers.Networking.DefaultGuestPipeName))
            {
                Assert.Fail("Server not found.");
                return;
            }

            bool operationCompleted = false;
            bool operationResult = false;
            string recivedMessage = null;

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create new processor that provide get token receiving operation.
                BaseQueries.GET_GUEST_TOKEN.GuestTokenProcessor processor = new BaseQueries.GET_GUEST_TOKEN.GuestTokenProcessor();

                // Sign up on callback that would be called when logon operation would be passed.
                processor.ProcessingFinished += delegate (
                    UniformQueries.Executable.QueryProcessor _,
                    bool result,
                    object message)
                {
                    // Buferize data.
                    operationResult = result;
                    recivedMessage = message as string;

                    // Continue thread.
                    operationCompleted = true;
                };

                // Request logon.
                processor.TryToReciveTokenAsync(
                    "localhost",
                    Helpers.Networking.DefaultGuestPipeName,
                    AuthorityController.Session.Current.TerminationTokenSource.Token);

                // Wait until logon would compleated.
                while (!operationCompleted)
                {
                    Thread.Sleep(5);
                }

                // Assert result based on received answer.
                Assert.IsTrue(operationResult, recivedMessage);
            }
        }
    }
}
