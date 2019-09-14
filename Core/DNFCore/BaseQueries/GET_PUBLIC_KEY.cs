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
using System.Text;
using System.IO;
using UniformQueries;
using UniformQueries.Executable;

namespace BaseQueries
{
    /// <summary>
    /// Query that request from server public encription key (RSA algorithm).
    /// </summary>
    class GET_PUBLIC_KEY : IQueryHandler
    {
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
                            "\tQUERY FORMAT: q=GET" + API.SPLITTING_SYMBOL + "sq=PUBLICKEY\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        public void Execute(QueryPart[] queryParts)
        {
            // Create public key as answer.
            string publicKey = "pk=" + PipesProvider.Security.Crypto.SerializePublicKey();

            // Set time when this key will expired.
            string expireTime = "expire=" + PipesProvider.Security.Crypto.RSAKeyExpireTime.ToBinary().ToString();

            // Compine answer.
            string answer = publicKey + API.SPLITTING_SYMBOL + expireTime;

            // Open answer chanel on server and send message.
            UniformServer.BaseServer.SendAnswerViaPP(answer, queryParts);
        }

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(QueryPart[] queryParts)
        {
            // Get query header.
            if (!API.TryGetParamValue("q", out QueryPart query, queryParts))
                return false;

            // Get subquery.
            if (!API.TryGetParamValue("sq", out QueryPart subQuery, queryParts))
                return false;

            // Check query.
            if (!query.ParamValueEqual("GET"))
                return false;

            // Check sub query.
            if (!subQuery.ParamValueEqual("PUBLICKEY"))
                return false;

            return true;
        }
    }
}
