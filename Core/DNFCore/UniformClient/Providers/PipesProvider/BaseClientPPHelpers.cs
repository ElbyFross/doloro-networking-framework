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
    /// Part of class that provide methods that simplify using of client.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Table that contain delegatds subscribed to beckward lines in duplex queries.
        /// 
        /// Key string - backward domain
        /// Value void(PipesProvider.Client.TransmissionLine, object) // answer processing delegat.
        /// </summary>
        protected static Hashtable DuplexBackwardCallbacks = new Hashtable();

        /// <summary>
        /// Event that would be called when duplex callback will received.
        /// Sharint transmisson GUID as parameter.
        /// </summary>
        public static Action<string> eDuplexBackwardCallbacksReceived;

        #region Output transmission
        /// <summary>
        /// Oppening transmition line that will able to send querie to described server's pipe.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="pipeName"></param>
        /// <returns></returns>
        public static TransmissionLine OpenOutTransmissionLineViaPP(
           string serverName,
           string pipeName)
        {
            return OpenTransmissionLineViaPP(serverName, pipeName,
                HandlerOutputTransmisssionAsync);
        }
        #endregion

        #region Input transmisssion
        /// <summary>
        /// Open a line that will be ready to recive server answer.
        /// New line will created related to params of requesting line and sended query.
        /// 
        /// Attention: Not work with broadcasting server.
        /// </summary>
        /// <param name="line">Line that was used to transmition</param>
        /// <param name="answerHandler">Delegate that will be called as handler for answer processing. 
        /// TransmissionLine contain data about actual transmission.
        /// object contain recived query (usualy string or byte[]).</param>
        /// <param name="entryQuery">Query that was recived from client. 
        /// Method will detect core part and establish backward connection.</param>
        /// <returns></returns>
        public static bool ReceiveDelayedAnswerViaPP(
            TransmissionLine line,
            UniformQueries.Query entryQuery,
            Action<TransmissionLine, UniformQueries.Query> answerHandler)
        {
            #region Create backward domain
            // Try to compute bacward domaint to contact with client.
            if (!UniformQueries.QueryPart.TryGetBackwardDomain(entryQuery, out string domain))
            {
                Console.WriteLine("ERROR (BCRA0): Unable to buid backward domain. QUERY: " + entryQuery.ToString());
                return false;
            }
            #endregion

            #region Append answer handler to backward table.
            string hashKey = line.ServerName + "\\" + domain;
            // Try to load registred callback to overriding.
            if (DuplexBackwardCallbacks[hashKey] is
                System.Action<TransmissionLine, UniformQueries.Query> registredCallback)
            {
                DuplexBackwardCallbacks[hashKey] = answerHandler;
            }
            else
            {
                // Add colback to table as new.
                DuplexBackwardCallbacks.Add(hashKey, answerHandler);
            }
            #endregion

            #region Opening transmition line
            // Create transmission line.
            TransmissionLine lineProcessor = OpenTransmissionLineViaPP(
                new Standard.SimpleClient(),
                line.ServerName, domain,
                ref line.accessToken,
                HandlerInputTransmissionAsync);

            // Set input direction.
            lineProcessor.Direction = TransmissionLine.TransmissionDirection.In;
            #endregion

            // Skip line
            Console.WriteLine();
            return true;
        }
        #endregion

        #region Broadcast connection
        /// <summary>
        /// Recive message from broadcasting server.
        /// ATTENTION: Eould connect to server as guest user.
        /// </summary>
        /// <param name="serverName">Srver name or ip.</param>
        /// <param name="pipeName">Name of pipe started on server.</param>
        /// <param name="answerHandler">Delegate that would to call when message received.</param>
        /// <returns>Created line.</returns>
        public static TransmissionLine ReceiveAnonymousBroadcastMessage(
            string serverName, 
            string pipeName,
            Action<TransmissionLine, UniformQueries.Query> answerHandler)
        {
            #region Append answer handler to backward table.
            string hashKey = serverName + "\\" + pipeName;
            // Try to load registred callback to overriding.
            if (DuplexBackwardCallbacks[hashKey] is
                System.Action<TransmissionLine, UniformQueries.Query> registredCallback)
            {
                // Override current delegate.
                DuplexBackwardCallbacks[hashKey] = answerHandler;
            }
            else
            {
                // Add callback to table as new.
                DuplexBackwardCallbacks.Add(hashKey, answerHandler);
            }
            #endregion

            #region Opening transmition line
            // Create transmission line.
            Console.WriteLine("RABM: Oppening line to " + serverName + "/" + pipeName);
            TransmissionLine line = OpenTransmissionLineViaPP(
                serverName, pipeName,
                HandlerInputTransmissionAsync);

            // Set input direction.
            line.Direction = TransmissionLine.TransmissionDirection.In;
            #endregion

            // Skip line
            Console.WriteLine();

            // Return created line.
            return line;
        }
        #endregion

        #region Duplex (two-ways) quries API
        /// <summary>
        /// Add query to queue. 
        /// Open backward line that will call answer handler.
        /// </summary>
        /// <param name="line">Line proccessor that control queries posting to target server.</param>
        /// <param name="query">Query that will sent to server.</param>
        /// <param name="answerHandler">Callback that will recive answer.</param>
        public static void EnqueueDuplexQueryViaPP(
            TransmissionLine line,
            UniformQueries.Query query,
            System.Action<TransmissionLine, UniformQueries.Query> answerHandler)
        {
            // Add our query to line processor queue.
            line.EnqueueQuery(query);

            // Open backward chanel to recive answer from server.
            ReceiveDelayedAnswerViaPP(line, query, answerHandler);
        }

        /// <summary>
        /// Add query to queue. 
        /// Open backward line that will call answer handler.
        /// </summary>
        /// <param name="serverName">Name of the server. "." if local.</param>
        /// <param name="serverPipeName">Name of pipe provided by server.</param>
        /// <param name="query">Query that will sent to server.</param>
        /// <param name="answerHandler">Callback that will recive answer.</param>
        /// <returns>Established transmission line.</returns>
        public static TransmissionLine EnqueueDuplexQueryViaPP(
            string serverName,
            string serverPipeName,
            UniformQueries.Query query,
            System.Action<TransmissionLine, UniformQueries.Query> answerHandler)
        {
            // Open transmission line.
            TransmissionLine line = OpenOutTransmissionLineViaPP(serverName, serverPipeName);

            // Equeue query to line.
            EnqueueDuplexQueryViaPP(line, query, answerHandler);

            return line;
        }
        #endregion
    }
}
