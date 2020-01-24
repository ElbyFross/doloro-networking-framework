﻿//Copyright 2019 Volodymyr Podshyvalov
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
using System.Text;
using System.IO;
using UniformQueries;
using UniformQueries.Executable;
using PipesProvider.Server.TransmissionControllers;
using PipesProvider.Security.Encryption;

namespace BaseQueries
{
    /// <summary>
    /// Query that requests from a server a public encription key (asymmetric).
    /// </summary>
    public class GET_PUBLIC_KEY : IQueryHandler
    {
        /// <summary>
        /// Generates a query with a public key info.
        /// </summary>
        private static Query PKQuery
        {
            get
            {
                var query = new Query(
                    new QueryPart("pk", EnctyptionOperatorsHandler.AsymmetricEO.SharableData),
                    new QueryPart("expire", EnctyptionOperatorsHandler.AsymmetricEO.ExpiryTime.ToBinary()),
                    new QueryPart("operator", 
                        EnctyptionOperatorsHandler.GetOperatorCode
                        (EnctyptionOperatorsHandler.AsymmetricEO)));

                return query;
            }
        }

        /// <summary>
        /// Return the description relative to the lenguage code or default if not found.
        /// </summary>
        /// <param name="cultureKey">Key of target culture.</param>
        /// <returns>Description for relative culture.</returns>
        public string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "GET PUBLICKEY\n" +
                            "\tDESCRIPTION: Will return public RSA key of this server," +
                            "\n\tthat can be used to encrypt message before start transmission.\n" +
                            "\tQUERY FORMAT: GET & PUBLICKEY\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="query">Recived query.</param>
        public void Execute(object sender, Query query)
        {
            // Open answer chanel on server and send message.
            UniformServer.BaseServer.SendAnswerViaPP(PKQuery, query);
        }

        /// <summary>
        /// Building a message suitable for the bradcasting.
        /// </summary>
        /// <param name="_">Not using.</param>
        /// <returns>A Query with public key info</returns>
        public static byte[] ToBroadcastMessage(BroadcastTransmissionController _)
        {
            // Converting the query to the binary format.
            var binaryData = UniformDataOperator.Binary.BinaryHandler.ToByteArray(PKQuery);

            // Retunina converted data.
            return binaryData;
        }

        /// <summary>
        /// Checks by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="query">Recived query.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(Query query)
        {
            // Check query.
            if (!query.QueryParamExist("get"))
                return false;

            // Check sub query.
            if (!query.QueryParamExist("publickey"))
                return false;

            return true;
        }
    }
}