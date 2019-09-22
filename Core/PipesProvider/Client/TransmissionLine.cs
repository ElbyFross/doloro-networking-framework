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
using System.IO.Pipes;
using System.Threading;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PipesProvider.Networking.Routing;

namespace PipesProvider.Client
{ 
    /// <summary>
    /// Class that provide information about line between client and server.
    /// Provide API to easy control.
    /// Provide automatic services.
    /// </summary>
    public class TransmissionLine
    {
        #region Enums
        /// <summary>
        /// Difines direction of transmission.
        /// </summary>
        public enum TransmissionDirection
        {
            /// <summary>
            /// Transmission would recive message from sender.
            /// </summary>
            In,
            /// <summary>
            /// Trasmission would emmite messages.
            /// </summary>
            Out
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Unique GUID for this pipe.
        /// </summary>
        public string GUID
        {
            get
            {
                // Generate GUID if not found.
                if (string.IsNullOrEmpty(guid))
                    guid = GenerateGUID(ServerName, ServerPipeName);
                return guid;
            }
        }
        private string guid = null;

        /// <summary>
        /// Name of server pipe that will be using for transmission via current processor.
        /// </summary>
        public string ServerPipeName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Name of server pipe that will be using for transmission via current processor.
        /// </summary>
        public string ServerName
        {
            get;
            protected set;
        }

        /// <summary>
        /// If true then this line will be closed on the next client tick.
        /// </summary>
        public bool Closed
        {
            get;
            protected set;
        }

        /// <summary>
        /// True if async operation started and not finished.
        /// </summary>
        public bool Processing
        {
            get;
            set;
        }

        /// <summary>
        /// Return tthe query that was dequeue at last.
        /// </summary>
        public QueryContainer LastQuery
        {
            get
            {
                return lastQuery.IsEmpty ? QueryContainer.Empty : QueryContainer.Copy(lastQuery);
            }
            protected set
            {
                lastQuery = value;
            }
        }

        /// <summary>
        /// Token that will used to autorizing on the server.
        /// </summary>
        public SafeAccessTokenHandle accessToken;

        /// <summary>
        /// Contain logon config to remote machine access.
        /// Contain RSA encryption keys data reklative to this line.
        /// </summary>
        public Instruction RoutingInstruction
        {
            get; protected set;
        }

        /// <summary>
        /// Marker that show does logon already finished.
        /// By default is true, cause default logon is anonymous.
        /// </summary>
        public bool LogonFinished
        { get; protected set; } = true;

        /// <summary>
        /// Define bihavior of the client loop.
        /// 
        /// In - will connect to target pipe as soon as possible.
        /// Out - will wait for query in queue.
        /// </summary>
        public TransmissionDirection Direction
        {
            get; set;
        } = TransmissionDirection.Out;

        /// <summary>
        /// Ecription provider that would applied to that transmission.
        /// </summary>
        public Security.Encryption.IEncryptionOperator TransmissionEncryption { get; set; } =
            new Security.Encryption.AESEncryptionOperator();
        #endregion

        #region Public fields
        /// <summary>
        /// Reference to the current oppened pipe.
        /// </summary>
        public NamedPipeClientStream pipeClient;

        /// <summary>
        /// This delegate will be callback when connection wfor qury will be established.
        /// </summary>
        public System.Action<TransmissionLine> queryProcessor;
        #endregion

        #region Protected fields
        /// <summary>
        /// Field that contain last dequeued query.
        /// </summary>
        protected QueryContainer lastQuery = QueryContainer.Empty;

        /// <summary>
        /// List of queries that will wait its order to access transmission via this line.
        /// </summary>
        protected Queue<QueryContainer> queries = new Queue<QueryContainer>();
        #endregion


        #region Constructors
        /// <summary>
        /// Create new instance of LineProcessor taht can be registread in static services.
        /// Contain information about transmission between client and server.
        /// </summary>
        /// <param name="serverName">Name of server into the network. If local than place "."</param>
        /// <param name="serverPipeName">Name of the pipe that will be used for transmitiong.</param>
        /// <param name="queryProcessor">Delegate that will be called when connection will be established.</param>
        /// <param name="token">Reference to token that provide authority to work with remote server.</param>
        public TransmissionLine(string serverName, string serverPipeName, System.Action<TransmissionLine> queryProcessor, ref SafeAccessTokenHandle token)
        {
            // Set fields.
            ServerName = serverName;
            ServerPipeName = serverPipeName;
            this.queryProcessor = queryProcessor;
            this.accessToken = token;

            // Registrate at hashtable.
            ClientAPI.TryToRegisterTransmissionLine(this);
        }

        /// <summary>
        /// Create instance using routing instruction.
        /// </summary>
        /// <param name="instruction">Routing insturuction that contain all data about target srver.</param>
        /// <param name="queryProcessor">Delegate that will be called when connection will be established.</param>
        public TransmissionLine(ref Instruction instruction, System.Action<TransmissionLine> queryProcessor)
        {
            // Set fields.
            RoutingInstruction = instruction;
            ServerName = instruction.routingIP;
            ServerPipeName = instruction.pipeName;
            this.queryProcessor = queryProcessor;

            // Logon as requested.
            TryLogonAs(instruction.logonConfig);

            // Registrate at hashtable.
            ClientAPI.TryToRegisterTransmissionLine(this);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Incremet of attempts count.
        /// </summary>
        /// <param name="line">Target line.</param>
        /// <returns></returns>
        public static TransmissionLine operator ++(TransmissionLine line)
        {
            line.lastQuery++;
            return line;
        }
        #endregion


        #region Queue API
        /// <summary>
        /// Enqueue query to order. Query will be posted to server as soon as will possible.
        /// </summary>
        /// <param name="query"></param>
        public TransmissionLine EnqueueQuery(string query)
        {
            queries.Enqueue(new QueryContainer(query, null));
            return this;
        }

        /// <summary>
        /// Enqueue query to order. Query will be posted to server as soon as will possible.
        /// </summary>
        /// <param name="query"></param>
        public TransmissionLine EnqueueQuery(QueryContainer query)
        {
            queries.Enqueue(query);
            return this;
        }

        /// <summary>
        /// Try to get a new query in turn.
        /// 
        /// Will return false if query not found.
        /// Will return false in case if LineProccessor has status InProgress.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool TryDequeQuery(out QueryContainer query)
        {
            // If some query already started then reject operation.
            if (Processing)
            {
                query = QueryContainer.Empty;
                return false;
            }

            try
            {
                // Dequeue query
                QueryContainer dequeuedQuery = queries.Dequeue();

                // Buferize at last.
                LastQuery = dequeuedQuery;

                // Initialize answer.
                query = dequeuedQuery;

                // Mark processor as busy.
                Processing = true;

                Console.WriteLine("QUERY DEQUEUED: {0}", LastQuery);

                // inform about success.
                return true;
            }
            catch (Exception ex)
            {
                // Inform about error during request.
                Console.WriteLine("LINE PROCESSOR ERROR (GUID: \"{3}\" ADDRESS: {0}/{1}): {2}",
                    ServerName, ServerPipeName, ex.Message, GUID);

                // Drop relative data.
                LastQuery = QueryContainer.Empty;
                query = QueryContainer.Empty;

                // Infor about failure.
                return false;
            }

        }

        /// <summary>
        /// Return true if queue contain queries.
        /// </summary>
        public bool HasQueries
        { get {  return queries.Count > 0; } }
        #endregion

        #region Finilizing API
        /// <summary>
        /// Mark line as closed. Thread will be terminated on the next client tick.
        /// </summary>
        public void Close()
        {
            // Mark as closed.
            Closed = true;

            // Drop processing marker to allow loop drop waiting to async operrations.
            Processing = false;

            // Log about closing
            Console.WriteLine("LINE CLOSED: " + ServerName + "/" + ServerPipeName);

            // Remove from table.
            ClientAPI.TryToUnregisterTransmissionLine(GUID);
        }

        /// <summary>
        /// Drop meta data relative only per one session.
        /// </summary>
        public void DropMeta()
        {
            pipeClient = null;
            Processing = false;
        }
        #endregion

        #region Remote machine LSA API
        /// <summary>
        /// Trying to logon using provided information.
        /// In case failed - close line.
        /// </summary>
        /// <param name="logonMeta"></param>
        /// <returns>Result of logon.</returns>
        public bool TryLogonAs(Security.LogonConfig logonMeta)
        {
            // Disable prmition to start.
            LogonFinished = false;

            //Console.WriteLine("{0}/{1}: LOGON STARTED", ServerName, ServerPipeName);

            // Try to logon using provided config.
            bool logonResult = Security.General.TryLogon(logonMeta, out SafeAccessTokenHandle safeTokenHandle);
            if (!logonResult)
            {
                // Log about error.
                Console.WriteLine("Logon failed. Connection not possible.");

                // Close line.
                Close();

                // inform about fail.
                return false;
            }
            else
            {
                // Save token as actual.
                accessToken = safeTokenHandle;
                
                // Change marker.
                LogonFinished = true;

                // Log about success.
                //Console.WriteLine("{0}/{1}: LOGON FINISHED {2}", ServerName, ServerPipeName, accessToken.GetHashCode());

                // Inform about success.
                return true;
            }
        }
        #endregion

        #region Routing instructions API
        /// <summary>
        /// Set routing instruction to line.
        /// Provide access to auto messages encryption with control of keys expiring.
        /// 
        /// ATTENTION: Line will not change logon config or server data. 
        /// If you want get full sync with routing instruction then user relative constructor.
        /// </summary>
        /// <param name="instruction">Instruction that will ocntain valid RSA key.</param>
        /// <returns></returns>
        public TransmissionLine SetInstructionAsKey(ref Instruction instruction)
        {
            // Update data.
            RoutingInstruction = instruction;
            
            // Try to logon as requested to recive token.
            //TryLogonAs(instruction.logonConfig);
            return this;
        }
        #endregion


        #region Static API
        /// <summary>
        /// Method that can be started as thread. Will start client loop.
        /// </summary>
        /// <param name="lineProcessor"></param>
        public static void ThreadLoop(object lineProcessor)
        {
            // Drop if incorrect argument.
            if (!(lineProcessor is TransmissionLine line))
            {
                Console.WriteLine("THREAD NOT STARTED. INVALID ARGUMENT.");
                return;
            }
                
            // Change thread cuture.
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Console.WriteLine("TL THREAD STARTED: {0}", Thread.CurrentThread.Name);

            // Wait until logon will finished.
            while(!line.LogonFinished)
            {
                Thread.Sleep(5);
            }

            // Drop if logon was failed.
            if(line.Closed)
            {
                return;
            }

            // Apply rights for connection.
            WindowsIdentity.RunImpersonated(line.accessToken, () =>
            {
                // Start client loop.
                ClientAPI.ClientLoop(
                    line,
                    System.IO.Pipes.PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            });
        }

        /// <summary>
        /// Generate GUID of this transmission line relative to pipe params.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="pipeName"></param>
        /// <returns></returns>
        public static string GenerateGUID(string serverName, string pipeName)
        {
            if(string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine("ERROR (TL GUID): Server name can't be null or empty.");
                return null;
            }

            if (string.IsNullOrEmpty(pipeName))
            {
                Console.WriteLine("ERROR (TL GUID): Pipe name can't be null or empty.");
                return null;
            }
            //return serverName.GetHashCode() + "_" + pipeName.GetHashCode();
            return serverName + "." + pipeName;
        }
        #endregion
    }
}