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
    /// Class that provide common methods for easy work with pipes' tasks.
    /// </summary>
    public static class ServerAPI
    {
        #region Events
        /// <summary>
        /// Event that will be called when server transmission will be registred or updated.
        /// </summary>
        public static event System.Action<BaseServerTransmissionController> ServerTransmissionMeta_InProcessing;
        #endregion

        #region Fields
        /// <summary>
        /// Hashtable thast contain references to oppened pipes.
        /// Key (string) pipe_name;
        /// Value (ServerTransmissionMeta) meta data about transmition.
        /// </summary>
        private static readonly Hashtable openedServers = new Hashtable();
        
        /// <summary>
        /// Return count of started threads.
        /// </summary>
        public static int SeversThreadsCount
        {
            get { return openedServers.Count; }
        }
        #endregion

        #region Core configurable loop
        /// <summary>
        /// Provide base server loop that control pipe.
        /// Have ability to full controll all handlers.
        /// 
        /// Warrning: Use only if undersend how it work. Overwise use simplived ClientToServerLoop or ServerToClientLoop
        /// </summary>
        /// <param name="guid">GUID that would be used to registration of that pipe.</param>
        /// <param name="connectionCallback">Delegate that will be called when connection will be established.</param>
        /// <param name="pipeName">Name of pipe that will be used to acces by client.</param>
        /// <param name="pipeDirection">Dirrection of the possible transmission.</param>
        /// <param name="allowedServerInstances">How many server pipes can be started with the same name.</param>
        /// <param name="transmissionMode">Type of transmission.</param>
        /// <param name="pipeOptions">Configuration of the pipe.</param>
        /// <param name="securityLevel">Security options that would be applied to this pipe.</param>
        /// <param name="initHandler">Handler that will be called in case if transmisssion still not registred.
        /// Provide possibility to castom initialization for every type of controller.</param>
        public static void ServerLoop<TransmissionControllerType>(
            string guid,
            System.Action<BaseServerTransmissionController> connectionCallback,
            string pipeName,
            PipeDirection pipeDirection,
            int allowedServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions pipeOptions,
            Security.SecurityLevel securityLevel,
            System.Action<BaseServerTransmissionController>initHandler = null)
            where TransmissionControllerType : BaseServerTransmissionController
        {
            // Create PipeSecurity relative to requesteed level.
            PipeSecurity pipeSecurity = Security.General.GetRulesForLevels(securityLevel);

            // Try to open pipe server.
            NamedPipeServerStream pipeServer = null;
            try
            {
                pipeServer =
                    new NamedPipeServerStream(pipeName, pipeDirection, allowedServerInstances,
                        transmissionMode, pipeOptions, 0, 0, pipeSecurity);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{1}: SERVER LOOP NOT STARTED:\n{0}\n", ex.Message, pipeName);
                return;
            }

            //Console.WriteLine("{0}: Pipe created", pipeName);

            #region Meta data
            // Meta data about curent transmition.
            TransmissionControllerType transmisssionController = null;
            IAsyncResult connectionMarker = null;

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
                ServerTransmissionMeta_InProcessing?.Invoke(transmisssionController);
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
                if ((connectionMarker == null || connectionMarker.IsCompleted) &&
                    !pipeServer.IsConnected)
                {
                    try
                    {
                        // Start async waiting of connection.
                        connectionMarker = pipeServer.BeginWaitForConnection(
                            Handlers.Service.ConnectionEstablishedCallbackRetranslator, 
                            transmisssionController);

                        // Update data.
                        transmisssionController.connectionMarker = connectionMarker;

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
                        transmisssionController.pipeServer = pipeServer;
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

            Console.WriteLine("{0}: PIPE SERVER CLOSED", transmisssionController.pipeName);
        }
        #endregion

        #region Controls
        /// <summary>
        /// Marking pipe as expired. 
        /// On the next loop tick connections will be disconnect and pipe will close.
        /// </summary>
        /// <param name="pipeName"></param>
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
        /// <param name="meta">Object that contains information about transmission.</param>
        public static void SetExpired(BaseServerTransmissionController meta)
        {
            // Mark it as expired.
            meta.SetExpired();
        }

        /// <summary>
        /// Markin all pipes as expired. 
        /// Connection will be terminated.
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
        /// Stop server by pipe name.
        /// </summary>
        /// <param name="pipeName"></param>
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
                openedServers.Remove(meta.pipeName);
            }
        }

        /// <summary>
        /// Stop server by relative meta data.
        /// </summary>
        /// <param name="meta"></param>
        public static void StopServer(BaseServerTransmissionController meta)
        {
            // If transmission has been opening.
            if (meta != null)
            {
                // Disconnect and close pipe.
                try
                {
                    // Disconnects clients.
                    if (meta.pipeServer.IsConnected)
                    {
                        meta.pipeServer.Disconnect();
                    }

                    // Closing pipe.
                    meta.pipeServer.Close();
                }
                catch (Exception ex)
                {
                    // Log error.
                    Console.WriteLine("SERVER STOP FAILED: {0}", ex.Message);
                }

                Console.WriteLine("PIPE CLOSED: {0}", meta.pipeName);
                return;
            }

            Console.WriteLine("META NOT FOUND");
        }

        /// <summary>
        /// Stoping all regirated servers.
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
        /// Try to find opened servert to client transmisssion meta data in common table.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="meta"></param>
        /// <returns></returns>
        public static bool TryGetServerTransmissionController(string guid, out BaseServerTransmissionController meta)
        {
            meta = openedServers[guid] as BaseServerTransmissionController;
            return meta != null;
        }
    }
}