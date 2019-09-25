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
using System.Threading.Tasks;
using System.Text;
using UniformQueries.Executable;

namespace UniformQueries
{
    /// <summary>
    /// Class that provide methods for handling of queries.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Symbol that divide query to parameters array.
        /// </summary>
        public const char SPLITTING_SYMBOL = '&';

        /// <summary>
        /// List that contain references to all query's handlers instances.
        /// </summary>
        public static List<IQueryHandler> QueryHandlers
        {
            get
            {
                return queryHandlers;
            }
        }
        private static readonly List<IQueryHandler> queryHandlers = null;

        /// <summary>
        /// Load query handlers during first call.
        /// </summary>
        static API()
        {
            // Init query handlers list.
            queryHandlers = new List<IQueryHandler>();

            // Load query's processors.
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //Console.WriteLine("ASSEMBLIES PROCEED: {0}\n", assemblies.Length);
            Console.WriteLine("\nDETECTED QUERIES:");
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();

                    // Get all types for assembly.
                    foreach (Type type in assembly.GetTypes())
                    {
                        try
                        {
                            // Check if this type is subclass of query.
                            if (type.GetInterface(typeof(IQueryHandler).FullName) != null)
                            {
                                // Instiniating querie processor.
                                IQueryHandler instance = (IQueryHandler)Activator.CreateInstance(type);
                                queryHandlers.Add(instance);
                                Console.WriteLine("{0}", type.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Queries asseblies loading failed (qapi10): {0}", ex.Message);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Queries asseblies loading failed (2): {qapi20}", ex.Message);
                }
            }

            // Log
            Console.WriteLine("\nRESUME:\nQueriesMonitor established. Session started at {0}\nTotal query processors detected: {1}",
                DateTime.Now.ToString("HH:mm:ss"), queryHandlers.Count);
        }

        /// <summary>
        /// Try to find requested param's value in query.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Obsolete]
        public static bool TryGetParamValue(string param, out string value, string query)
        {
            // Try to find start index of query part.
            int startIndex = query.IndexOf(param + "=");
            // Drop if not found.
            if(startIndex == -1)
            {
                value = null;
                return false;
            }

            // looking for part's end symbol.
            int endIndex = query.IndexOf("&", startIndex);

            // Make offset to value start position.
            startIndex += param.Length + 1;

            // Copying value.
            value = query.Substring(startIndex,
                endIndex == -1 ? query.Length - startIndex : endIndex - startIndex);

            return true;
        }

        /// <summary>
        /// Try to find requested all param's value among query parts by requested param name.
        /// </summary>
        /// <param name="param">Target param's name.</param>
        /// <param name="queryParts">Array with query parts.</param>
        /// <returns>Suitable query parts with target param.</returns>
        public static List<QueryPart> GetParamValues(string param, params QueryPart[] queryParts)
        {
            List<QueryPart> value = new List<QueryPart>();
            // Try to find target param
            foreach (QueryPart part in queryParts)
            {
                // If target param
                if (part.ParamNameEqual(param))
                {
                    // Get value.
                    value.Add(part);
                }
            }
            return value;
        }

        /// <summary>
        /// Convert query's string to array of query parts.
        /// User SPLITTING_SYMBOL as spliter for detect query parts.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPart[] DetectQueryParts(string query)
        {
            return DetectQueryParts(query, SPLITTING_SYMBOL);
        }

        /// <summary>
        /// Convert query's string to array of query parts.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="spliter">Char that will be used as query part spliter.</param>
        /// <returns></returns>
        public static QueryPart[] DetectQueryParts(string query, char spliter)
        {
            // Get query parts in string format.
            string[] splitedQuery = query.Split(spliter);

            // Init list.
            QueryPart[] parts = new QueryPart[splitedQuery.Length];

            // Add parts to array. Will auto converted from string to QueryPart.
            for (int i = 0; i < splitedQuery.Length; i++)
            {
                parts[i] = (QueryPart)splitedQuery[i];
            }

            return parts;
        }

        /// <summary>
        /// Looking for query handler.
        /// </summary>
        /// <param name="query">Recived query.</param>
        /// <param name="handler">Hadler that's situated to this query.</param>
        /// <returns></returns>
        public static bool TryFindQueryHandler(Query query, out IQueryHandler handler)
        {
            foreach (IQueryHandler pb in UniformQueries.API.QueryHandlers)
            {
                // Check header
                if (pb.IsTarget(query))
                {
                    handler = pb;
                    return true;
                }
            }

            handler = null;
            return false;
        }
    }
}
