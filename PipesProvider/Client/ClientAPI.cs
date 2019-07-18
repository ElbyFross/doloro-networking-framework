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
        private static readonly Hashtable openedClients = new Hashtable();
        
        /// <summary>
        /// Start saftely async waiting connection operation.
        /// </summary>
        /// <param name="pipeClient"></param>
        /// <param name="lineProcessor"></param>
        async static void ConnectToServerAsync(
            NamedPipeClientStream pipeClient,
            TransmissionLine lineProcessor)
        {
            try
            {
                // Wait until connection.
                await pipeClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Console.WriteLine("{0}/{1}: ERROR Connection not possible (CtSAsync0).\nFAILURE REASON: \n{2}",
                    lineProcessor.ServerName, lineProcessor.ServerPipeName, ex.Message + "\n" + ex.StackTrace);
            }
        }


        #region Public methods
        /// <summary>
        /// Provide stable client loop controlled by data of LineProcessor.
        /// </summary>
        /// <param name="lineProcessor"></param>
        /// <param name="pipeDirection"></param>
        /// <param name="pipeOptions"></param>
        public static void ClientLoop(
            TransmissionLine lineProcessor,
            PipeDirection pipeDirection,
            PipeOptions pipeOptions
            )
        {
            // Loop will work until this proceesor line not closed.
            while (!lineProcessor.Closed)
            {
                // In case if line in out transmission mode.
                // If queries not placed then wait.
                while 
                    (
                    lineProcessor.Direction == TransmissionLine.TransmissionDirection.Out &&
                    (!lineProcessor.HasQueries || !lineProcessor.TryDequeQuery(out _))
                    )
                {
                    Thread.Sleep(50);
                    continue;
                }

                // Open pipe.
                using (NamedPipeClientStream pipeClient =
                    new NamedPipeClientStream(
                        lineProcessor.ServerName,
                        lineProcessor.ServerPipeName,
                        pipeDirection, pipeOptions,
                        System.Security.Principal.TokenImpersonationLevel.Impersonation,
                        HandleInheritability.None))
                {
                    // Update meta data.
                    lineProcessor.pipeClient = pipeClient;

                    // Log.
                    Console.WriteLine("{0}/{1} (CL0): Connection to server.", lineProcessor.ServerName, lineProcessor.ServerPipeName);

                    // Request connection.
                    ConnectToServerAsync(pipeClient, lineProcessor);

                    // Sleep until connection.
                    while (!pipeClient.IsConnected)
                    {
                        Thread.Sleep(50);
                    }

                    // Log about establishing.
                    Console.WriteLine("{0}/{1}: Connection established.", lineProcessor.ServerName, lineProcessor.ServerPipeName);

                    try
                    {
                        // Execute target query.
                        lineProcessor.queryProcessor?.Invoke(lineProcessor);

                        // Wait until processing finish.
                        Console.WriteLine("{0}/{1}: WAIT UNITL QUERY PROCESSOR FINISH HANDLER.", lineProcessor.ServerName, lineProcessor.ServerPipeName);
                        while (lineProcessor.Processing)
                        {
                            Thread.Sleep(50);
                        }
                        Console.WriteLine("{0}/{1}: WAIT UNITL QUERY PROCESSOR HANDLER FINISHED.", lineProcessor.ServerName, lineProcessor.ServerPipeName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}/{1}: EXECUTION TIME ERROR. {2}", lineProcessor.ServerName, lineProcessor.ServerPipeName, ex.Message);
                    }

                    // Log about establishing.
                    Console.WriteLine("{0}/{1}: Transmission finished at {2}.", lineProcessor.ServerName, lineProcessor.ServerPipeName, DateTime.Now.ToString("HH:mm:ss.fff"));

                    // Remove not relevant meta data.
                    lineProcessor.pipeClient.Dispose();
                    lineProcessor.DropMeta();

                    Console.WriteLine();
                }

                // Let other threads time for processing before next query.
                Thread.Sleep(50);
            }
        }

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
            // Build pipe domain.
            string lineDomain = lineProcessor.ServerName + "." + lineProcessor.ServerPipeName;

            // Reject if already registred.
            if (openedClients[lineDomain] is TransmissionLine)
            {
                Console.WriteLine("LINE PROCESSOR \"{0}\" ALREADY EXIST.", lineDomain);
                return false;
            }

            // Add line to table.
            openedClients.Add(lineDomain, lineProcessor);
            return true;
        }

        /// <summary>
        /// Remove line from table if this line closed.
        /// In other keys this operation not available due to security amd stability purposes.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static bool TryToUnregisterTransmissionLine(string guid)
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

        /// <summary>
        /// Closing all lines registred in table.
        /// </summary>
        public static void CloseAllTransmissionLines()
        {
            // Closing every line.
            foreach(TransmissionLine line in openedClients.Values)
            {
                line.Close();
            }

            // Clear garbage.
            openedClients.Clear();
        }
        #endregion
    }
}