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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using PipesProvider.Networking;

namespace PipesProvider.Client
{
    /// <summary>
    /// A static API class that provides common methods for simplifying handle of client pipes' tasks.
    /// </summary>
    public static class ClientAPI
    {
        /// <summary>
        /// A hashtable that contains references to opened pipes.
        /// Key (string) server_name.pipe_name;
        /// Value (LineProcessor) meta data about transmition.
        /// </summary>
        private static readonly Hashtable openedClients = new Hashtable();
        
        /// <summary>
        /// Returns a count of started threads.
        /// </summary>
        public static int ThreadsCount
        {
            get { return openedClients.Count; }
        }

        #region Loops
        /// <summary>
        /// Provides a stable client loop controlled by data of the TransmissionLine.
        /// </summary>
        /// <param name="line">A line that will be handled by the loop.</param>
        /// <param name="pipeDirection">Direction of the pipe. Defines the loop's behavior model.</param>
        /// <param name="pipeOptions">Options that will be applied to a pipe client established for the line.</param>
        public static void ClientLoop(
            TransmissionLine line,
            PipeDirection pipeDirection,
            PipeOptions pipeOptions)
        {
            // Loop will work until this proceesor line not closed.
            while (!line.Closed)
            {
                // In case if line in out transmission mode.
                // If queries not placed then wait.
                while( line.Direction == TransmissionLine.TransmissionDirection.Out &&
                     (!line.HasQueries || !line.TryDequeQuery(out _)) )
                {
                    // Drop if closed.
                    if (line.Closed) return;
                    
                    Thread.Sleep(50);
                    continue;
                }

                // Skip connection in line interrupted before connecting.
                if (line.Interrupted) continue;

                // Open pipe.
                using (NamedPipeClientStream pipeClient =
                    new NamedPipeClientStream(
                        line.ServerName,
                        line.ServerPipeName,
                        pipeDirection, pipeOptions,
                        TokenImpersonationLevel.Impersonation,
                        HandleInheritability.None))
                {
                    // Update meta data.
                    line.pipeClient = pipeClient;

                    // Log.
                    Console.WriteLine("{0}/{1} (CL0): Connection to server.", line.ServerName, line.ServerPipeName);

                    // Wait until named pipe server would become exit.
                    while(!NativeMethods.DoesNamedPipeExist(
                        line.ServerName, line.ServerPipeName))
                    {
                        // Drop if not relevant.
                        if (line.Closed) return;

                        // Suspend thread if server not exist.
                        Thread.Sleep(50);
                    }

                    // Skip connection in line interrupted before connecting.
                    if (line.Interrupted) continue;

                    // Connect to server. Would suspend moving forward until connect establishing.
                    ConnectToServer(pipeClient);

                    // Log about establishing.
                    Console.WriteLine("{0}/{1}: Connection established.", line.ServerName, line.ServerPipeName);

                    try
                    {
                        // Execute target query.
                        line.queryHandler?.Invoke(line);

                        // Wait until processing finish.
                        Console.WriteLine("{0}/{1}: WAIT UNITL QUERY PROCESSOR FINISH HANDLER.", line.ServerName, line.ServerPipeName);
                        while (line.Processing) 
                        {
                            if(line.Interrupted)
                            {
                                break;
                            }

                            Thread.Sleep(50);
                        }
                        Console.WriteLine("{0}/{1}: QUERY PROCESSOR HANDLER FINISHED.", line.ServerName, line.ServerPipeName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}/{1}: EXECUTION TIME ERROR. {2}", line.ServerName, line.ServerPipeName, ex.Message);
                    }

                    // Log about establishing.
                    Console.WriteLine("{0}/{1}: Transmission finished at {2}.", 
                        line.ServerName, 
                        line.ServerPipeName, 
                        DateTime.Now.ToString("HH:mm:ss.fff"));

                    // Remove not relevant meta data.
                    line.pipeClient.Dispose();
                    line.DropMeta();

                    Console.WriteLine();
                }

                // Let other threads time for processing before next query.
                Thread.Sleep(50);
            }
        }
        #endregion

        #region Transmisssion line
        /// <summary>
        /// Try to find out registred transmission line by GUID at the <see cref="openedClients"/>.
        /// If client not strted then return false.
        /// </summary>
        /// <param name="guid">An unique GUID related to some registered transmission line.</param>
        /// <param name="line">A line with requested GUID.</param>
        /// <returns>A result of the operation.</returns>
        public static bool TryGetTransmissionLineByGUID(
            string guid, 
            out TransmissionLine line)
        {
            line = openedClients[guid] as TransmissionLine;
            return line != null;
        }

        /// <summary>
        /// Tries to register a transmission line in the <see cref="openedClients"/> by a key:
        /// ServerName.PipeName
        /// 
        /// If the line is already exist then returns false.
        /// Retruns success if added to the table.
        /// </summary>
        /// <param name="line">A line for handling.</param>
        /// <returns>A result of the operation.</returns>
        public static bool TryToRegisterTransmissionLine(TransmissionLine line)
        {
            lock (openedClients)
            {
                // Build pipe domain.
                string lineDomain = line.ServerName + "." + line.ServerPipeName;

                // Reject if already registred.
                if (openedClients[lineDomain] is TransmissionLine)
                {
                    Console.WriteLine("LINE PROCESSOR \"{0}\" ALREADY EXIST.", lineDomain);
                    line.Close();
                    return false;
                }

                // Add line to table.
                openedClients.Add(lineDomain, line);
                return true;
            }
        }

        /// <summary>
        /// Removes a line from the <see cref="openedClients"/> if this line closed.
        /// In other case this operation not available due to security and stability purposes.
        /// </summary>
        /// <param name="guid">A GUID of the line.</param>
        /// <returns>A result of the operation.</returns>
        public static bool TryToUnregisterTransmissionLine(string guid)
        {
            lock (openedClients)
            {
                // Reject if already registred.
                if (openedClients[guid] is TransmissionLine transmissionLine)
                {
                    // if not closed.
                    if (!transmissionLine.Closed)
                        return false;

                    // Remove from table.
                    openedClients.Remove(guid);
                }
                return true;
            }
        }

        /// <summary>
        /// Closes all the lines registered in the table.
        /// </summary>
        public static void CloseAllTransmissionLines()
        {
            lock (openedClients)
            {
                // Buferize list to prefent pointers errors.
                var bufer = new List<TransmissionLine>();
                foreach (TransmissionLine line in openedClients.Values)
                {
                    bufer.Add(line);
                }

                // Closing line droping the loop
                foreach (TransmissionLine line in bufer)
                {
                    line.Close();
                }

                // Clear garbage.
                openedClients.Clear();
            }
        }
        #endregion

        #region Connection
        /// <summary>
        /// Starts a safety async  operation of connection waiting.
        /// </summary>
        /// <param name="pipeClient">A target pipe client instance that will wait for a server.</param>
        public async static void ConnectToServerAsync(NamedPipeClientStream pipeClient)
        {
            await Task.Run(() =>
            {
                ConnectToServer(pipeClient);
            });
        }

        /// <summary>
        /// Connects to a server's pipe.
        /// If connection is failed then stop the thread.
        /// 
        /// Suspends a caller's thread.
        /// </summary>
        /// <param name="pipeClient">A pipe client that will be handled.</param>
        public static void ConnectToServer(NamedPipeClientStream pipeClient)
        {
            while (!pipeClient.IsConnected)
            {
                try
                {
                    pipeClient.Connect(15);
                }
                catch
                {
                    pipeClient.Dispose();
                    Thread.Sleep(2000);
                }
            }
        }
        #endregion
    }
}