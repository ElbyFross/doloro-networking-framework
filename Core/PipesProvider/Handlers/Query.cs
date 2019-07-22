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

namespace PipesProvider.Handlers
{
    public static class Query
    {
        /// <summary>
        /// Handler that can be connected as callback to default PipesProvides DNS Handler.
        /// Will validate and decompose querie on parts and send it to target QueryProcessor.
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="query"></param>
        public static async void ProcessingAsync(BaseServerTransmissionController _, string query)
        {
            // Detect query parts.
            QueryPart[] queryParts = UQAPI.DetectQueryParts(query);
            QueryPart token = QueryPart.None;

            // Try to detect target query processor.
            if(API.TryFindQueryHandler(queryParts, out UniformQueries.IQueryHandler handler))
            {
                // Log.
                Console.WriteLine("Start execution: [{0}]\n for token: [{1}]",
                    query, token.IsNone ? "token not found" : token.propertyValue);

                // Execute query as async.
                await Task.Run(() => handler.Execute(queryParts));
            }
            else
            {
                // Inform about error.
                Console.WriteLine("POST ERROR: Token: {1} | Handler for query \"{0}\" not implemented.",
                    query, token);
            }           
        }
    }
}
