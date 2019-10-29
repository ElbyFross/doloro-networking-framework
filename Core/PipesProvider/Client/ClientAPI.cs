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
    /// Class that provide common methods for easy work with pipes' tasks.
    /// </summary>
    public static class ClientAPI
    {
        /// <summary>
        /// Hashtable thast contain references to oppened pipes.
        /// Key (string) server_name.pipe_name;
        /// Value (LineProcessor) meta data about transmition.
        /// </summary>
        public static readonly Hashtable openedClients = new Hashtable();
        
        #region Loops
        /// <summary>
        /// Provide stable client loop controlled by data of LineProcessor.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pipeDirection"></param>
        /// <param name="pipeOptions"></param>
        public static void ClientLoop(
            TransmissionLine line,
            PipeDirection pipeDirection,
            PipeOptions pipeOptions
            )
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
                        line.queryProcessor?.Invoke(line);

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
        /// Try to find out registred transmission line by GUID.
        /// If client not strted then return false.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="lineProcessor"></param>
        /// <returns></returns>
        public static bool TryGetTransmissionLineByGUID(
            string guid, 
            out TransmissionLine lineProcessor)
        {
            lineProcessor = openedClients[guid] as TransmissionLine;
            return lineProcessor != null;
        }

        /// <summary>
        /// Trying to register transmission line in common table by key:
        /// ServerName.PipeLine
        /// 
        /// If exist return false.
        /// Retrun sycces if added to table.
        /// </summary>
        /// <param name="lineProcessor"></param>
        /// <returns></returns>
        public static bool TryToRegisterTransmissionLine(TransmissionLine lineProcessor)
        {
            lock(openedClients)
            {
                // Build pipe domain.
                string lineDomain = lineProcessor.ServerName + "." + lineProcessor.ServerPipeName;

                // Reject if already registred.
                if (openedClients[lineDomain] is TransmissionLine)
                {
                    Console.WriteLine("LINE PROCESSOR \"{0}\" ALREADY EXIST.", lineDomain);
                    lineProcessor.Close();
                    return false;
                }

                // Add line to table.
                openedClients.Add(lineDomain, lineProcessor);
                return true;
            }
        }

        /// <summary>
        /// Remove line from table if this line closed.
        /// In other keys this operation not available due to security amd stability purposes.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
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
        /// Closing all lines registred in table.
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
        /// Start saftely async waiting connection operation.
        /// </summary>
        /// <param name="pipeClient"></param>
        public async static void ConnectToServerAsync(NamedPipeClientStream pipeClient)
        {
            await Task.Run(() =>
            {
                ConnectToServer(pipeClient);
            });
        }

        /// <summary>
        /// Connecting to server's pipe.
        /// If connection failed them stop thread.
        /// 
        /// Suspend caller thread.
        /// </summary>
        /// <param name="pipeClient"></param>
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