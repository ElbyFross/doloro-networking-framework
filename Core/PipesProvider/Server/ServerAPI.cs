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
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using UniformQueries;
using UQAPI = UniformQueries.API;
using PipesProvider.Server.TransmissionControllers;

namespace PipesProvider.Server
{
    /// <summary>
    /// A static API class that provides common methods for simplifying handle of server pipes' tasks.
    /// </summary>
    public static class ServerAPI
    {
        /// <summary>
        /// An event that will occures when a server transmission will be registred or updated.
        /// </summary>
        public static event Action<BaseServerTransmissionController> TransmissionToProcessing;
        
        /// <summary>
        /// Returns a count of started threads.
        /// </summary>
        public static int ThreadsCount
        {
            get { return openedServers.Count; }
        }

        /// <summary>
        /// A hashtable that contains references to opened pipes.
        /// Key (string) pipe_name;
        /// Value (ServerTransmissionMeta) meta data about transmition.
        /// </summary>
        private static readonly Hashtable openedServers = new Hashtable();

        #region Core configurable loop
        /// <summary>
        /// Provides a base server loop that controls a pipe.
        /// Has ability to full control  all level of networking handlers.
        /// </summary>
        /// <param name="guid">An unique GUID that will be used to registration of a pipe.</param>
        /// <param name="connectionCallback">
        /// A delegate that will be called when connection 
        /// between the server an client will be established.
        /// </param>
        /// <param name="pipeName">A name of the pipe that will be used to access by clients.</param>
        /// <param name="pipeDirection">Dirrection of a data transmission.</param>
        /// <param name="allowedServerInstances">Count of server pipes those can be started with the same name.</param>
        /// <param name="transmissionMode">Specifies the transmission mode of the pipe.</param>
        /// <param name="pipeOptions">Configuration of the pipe.</param>
        /// <param name="securityLevel">A security lable that will be applied to the pipe.</param>
        /// <param name="initHandler">
        /// A handler that will be called in case if transmission still not registred.
        /// Provides possibility to fulfill custom initialization specified for a certain transmission controller.
        /// </param>
        public static void ServerLoop<TransmissionControllerType>(
            string guid,
            Action<BaseServerTransmissionController> connectionCallback,
            string pipeName,
            PipeDirection pipeDirection,
            int allowedServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions pipeOptions,
            Security.SecurityLevel securityLevel,
            Action<BaseServerTransmissionController> initHandler = null)
            where TransmissionControllerType : BaseServerTransmissionController
        {
            // Creating a PipeSecurity relative to requesteed level.
            PipeSecurity pipeSecurity = Security.General.GetRulesForLevels(securityLevel);

            // Trying to open a pipe server.
            NamedPipeServerStream pipeServer = null;
            try
            {
                pipeServer = new NamedPipeServerStream(
                    pipeName, pipeDirection, allowedServerInstances,
                    transmissionMode, pipeOptions, 0, 0, pipeSecurity);
            }
            catch (Exception ex)
            {
                pipeServer.Dispose();
                Console.WriteLine("{1}: SERVER LOOP NOT STARTED:\n{0}\n", ex.Message, pipeName);
                return;
            }

            //Console.WriteLine("{0}: Pipe created", pipeName);

            #region Meta data
            // Meta data about curent transmition.
            TransmissionControllerType transmisssionController = null;

            lock (openedServers)
            {
                // Registration or update controller of oppened transmission.
                if (openedServers[guid] is TransmissionControllerType bufer)
                {
                    // Load previous contorller.
                    transmisssionController = bufer;
                }
                else
                {
                    // Create new controller.
                    transmisssionController = (TransmissionControllerType)Activator.CreateInstance(
                        typeof(TransmissionControllerType),
                        new object[]
                        {
                            null,
                            connectionCallback,
                            pipeServer,
                            pipeName
                        });

                    // Call additive init.
                    initHandler?.Invoke(transmisssionController);

                    // Add to table.
                    openedServers.Add(guid, transmisssionController);
                }
            }

            try
            {
                // Inform subscribers about new pass with this transmission to give possibility correct data.
                TransmissionToProcessing?.Invoke(transmisssionController);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server Loop. Transmission event fail. (SLTEF0): {0}", ex.Message);
            }
            #endregion

            #region Main loop
            while (!transmisssionController.Expired)
            {
                // Wait for a client to connect
                if ((transmisssionController.connectionMarker == null || 
                    transmisssionController.connectionMarker.IsCompleted) &&
                    !pipeServer.IsConnected && 
                    transmisssionController.NewConnectionSearchAllowed)
                {
                    try
                    {
                        // Start async waiting of connection.
                        transmisssionController.connectionMarker = pipeServer.BeginWaitForConnection(
                            Handlers.Service.ConnectionEstablishedCallbackRetranslator,
                            transmisssionController);

                        // Update data.
                        transmisssionController.NewConnectionSearchAllowed = false;

                        Console.Write("{0}: Waiting for client connection...\n", pipeName);
                    }
                    catch //(Exception ex)
                    {
                        //Console.WriteLine("SERVER LOOP ERROR. SERVER RESTORED: {0}", ex.Message);

                        // Close actual pipe.
                        pipeServer.Close();

                        // Establish new server.
                        pipeServer = new NamedPipeServerStream(pipeName, pipeDirection, allowedServerInstances,
                        transmissionMode, pipeOptions, 0, 0, pipeSecurity);

                        // Update meta data.
                        transmisssionController.PipeServer = pipeServer;
                    }
                    //Console.WriteLine("TRANSMITION META HASH: {0}", meta.GetHashCode());
                }

                // Turn order to other threads.
                Thread.Sleep(150);
            }
            #endregion

            // Finalize server.
            if (!transmisssionController.Stoped)
            {
                StopServer(transmisssionController);
            }

            // Discharge existing in hashtable.
            openedServers.Remove(guid);

            // Finish stream.
            pipeServer.Close();
            pipeServer.Dispose();

            Console.WriteLine("{0}: PIPE SERVER CLOSED", transmisssionController.PipeName);
        }
        #endregion

        #region Controls
        /// <summary>
        /// Marks a pipe as expired. 
        /// At the next loop tick connections will be disconnected and pipe will be closed.
        /// </summary>
        /// <param name="pipeName">A pipe's name.</param>
        public static void SetExpired(string pipeName)
        {
            // Check meta data existing.
            if (openedServers.ContainsKey(pipeName))
            {
                // Load meta data.
                BaseServerTransmissionController meta = (BaseServerTransmissionController)openedServers[pipeName];

                // Mark it as expired.
                meta.SetExpired();
            }
        }

        /// <summary>
        /// Marking pipe as expired. 
        /// On the next loop tick connections will be disconnect and pipe will close.
        /// </summary>
        /// <param name="controller">A transmission controller that will expired.</param>
        public static void SetExpired(BaseServerTransmissionController controller)
        {
            // Mark it as expired.
            controller.SetExpired();
        }

        /// <summary>
        /// Marks all active pipes as expired. 
        /// The connections will be terminated.
        /// </summary>
        public static void SetExpiredAll()
        {
            lock (openedServers)
            {
                foreach (BaseServerTransmissionController meta in openedServers.Values)
                {
                    meta.SetExpired();
                }
            }
        }

        /// <summary>
        /// Stops a server by a pipe's name.
        /// </summary>
        /// <param name="pipeName">A pipe's name.</param>
        public static void StopServer(string pipeName)
        {
            // Check meta data existing.
            if (openedServers.ContainsKey(pipeName))
            {
                // Load meta data.
                BaseServerTransmissionController meta = (BaseServerTransmissionController)openedServers[pipeName];

                // Stop server relative to meta data.
                StopServer(meta);

                // Discharge existing in hashtable.
                openedServers.Remove(meta.PipeName);
            }
        }

        /// <summary>
        /// Stops a server by relative controller.
        /// </summary>
        /// <param name="controller">A trasmission controller for stop.</param>
        public static void StopServer(BaseServerTransmissionController controller)
        {
            // If transmission has been opening.
            if (controller != null)
            {
                // Disconnect and close pipe.
                try
                {
                    // Disconnects clients.
                    if (controller.PipeServer.IsConnected)
                    {
                        controller.PipeServer.Disconnect();
                    }

                    // Closing pipe.
                    controller.PipeServer.Close();
                }
                catch (Exception ex)
                {
                    // Log error.
                    Console.WriteLine("SERVER STOP FAILED: {0}", ex.Message);
                }

                Console.WriteLine("PIPE CLOSED: {0}", controller.PipeName);
                return;
            }

            Console.WriteLine("META NOT FOUND");
        }

        /// <summary>
        /// Stops all regirated servers.
        /// </summary>
        public static void StopAllServers()
        {
            lock (openedServers)
            {
                // Log statistic.
                Console.WriteLine("TRANSMISSIONS TO CLOSE: {0}", openedServers.Count);

                // Stop every registred server.
                foreach (BaseServerTransmissionController meta in openedServers.Values)
                {
                    // Log about target to close.
                    //Console.WriteLine("STOPING SERVER: {0}", meta.name);

                    // Mark as stoped.
                    meta.SetStoped();

                    // Stop server described by meta.
                    StopServer(meta);
                }

                // Clear hashtable with terminated servers.
                openedServers.Clear();
            }

            // Console output formating.
            Console.WriteLine();

            // Let servers time to finish up transmissions.
            Thread.Sleep(1000);
        }
        #endregion

        /// <summary>
        /// Tries to find opened server transmission metadata in the <see cref="openedServers"/> table.
        /// </summary>
        /// <param name="guid">An unique GUID of the line.</param>
        /// <param name="controller">A found transmission controller.</param>
        /// <returns>A result of the operation.</returns>
        public static bool TryGetServerTransmissionController(string guid, out BaseServerTransmissionController controller)
        {
            controller = openedServers[guid] as BaseServerTransmissionController;
            return controller != null;
        }
    }
}