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
using UniformQueries.Executable;
using UQAPI = UniformQueries.API;
using PipesProvider.Server.TransmissionControllers;

namespace PipesProvider.Handlers
{
    /// <summary>
    /// Class that provides handlers for working with network queries.
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Handler that can be connected as callback to default PipesProvides DNS Handler.
        /// Will validate and decompose querie on parts and send it to target Executable.QueryProcessor.
        /// </summary>
        /// <param name="tl">Server's transmission controller called that handler.</param>
        /// <param name="query">Received query</param>
        public static async void ProcessingAsync(BaseServerTransmissionController tl, UniformQueries.Query query)
        {
            // Detect query parts.
            query.TryGetParamValue("token", out QueryPart token);

            // Try to detect target query processor.
            if(API.TryFindQueryHandler(query, out IQueryHandler handler))
            {
                // Log.
                Console.WriteLine("Start execution: [{0}]\n for token: [{1}]",
                    query, token.IsNone ? "token not found" : token.PropertyValueString);

                // Execute query as async.
                await Task.Run(() => handler.Execute(tl, query));
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
