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
        /// Check existing of param in query parts.
        /// Example entry query part: "q=Get", where target param is "q".
        /// </summary>
        /// <param name="param"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool QueryParamExist(string param, string query)
        {
            return QueryParamExist(param, query.Split(SPLITTING_SYMBOL));
        }

        /// <summary>
        /// Check existing of param in query parts.
        /// Example entry query part: "q=Get", where target param is "q".
        /// </summary>
        /// <param name="param"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static bool QueryParamExist(string param, params string[] queryParts)
        {
            // Try to find target param
            foreach (string part in queryParts)
            {
                // If target param
                if (part.StartsWith(param + "=")) return true;
            }
            return false;
        }

        /// <summary>
        /// Check existing of param in query parts.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static bool QueryParamExist(string param, params QueryPart[] queryParts)
        {
            // Try to find target param
            foreach (QueryPart part in queryParts)
            {
                // If target param
                if (part.ParamNameEqual(param))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Try to find requested param's value in query.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool TryGetParamValue(string param, out string value, string query)
        {
            return TryGetParamValue(param, out value, query.Split(SPLITTING_SYMBOL));
        }

        /// <summary>
        /// Try to find requested param's value among query parts.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static bool TryGetParamValue(string param, out string value, params string[] queryParts)
        {
            // Try to find target param
            foreach (string part in queryParts)
            {
                // If target param
                if (part.StartsWith(param + "="))
                {
                    // Get value.
                    value = part.Substring(param.Length + 1);
                    // Mark as success.
                    return true;
                }
            }

            // Inform that param not found.
            value = null;
            return false;
        }

        /// <summary>
        /// Try to find requested param's value among query parts.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static bool TryGetParamValue(string param, out QueryPart value, params QueryPart[] queryParts)
        {
            // Try to find target param
            foreach (QueryPart part in queryParts)
            {
                // If target param
                if (part.ParamNameEqual(param))
                {
                    // Get value.
                    value = part;
                    // Mark as success.
                    return true;
                }
            }

            // Inform that param not found.
            value = QueryPart.None;
            return false;
        }


        /// <summary>
        /// Try to find requested all param's value among query parts by requested param name.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static List<string> GetParamValues(string param, params string[] queryParts)
        {
            List<string> value = new List<string>();
            // Try to find target param
            foreach (string part in queryParts)
            {
                // If target param
                if (part.StartsWith(param + "="))
                {
                    // Get value.
                    value.Add(part.Substring(param.Length + 1));
                }
            }
            return value;
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
        /// Build query string with requested parts and core data.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        public static string MakeQuery(string guid, string token, params QueryPart[] queryParams)
        {
            string query = "";

            // Build core data.
            query += new QueryPart("guid", guid);
            query += SPLITTING_SYMBOL;
            query += new QueryPart("token", token);

            // Add parts.
            foreach(QueryPart part in queryParams)
                query += SPLITTING_SYMBOL + part;

            return query;
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
        /// Looking for processor situable for provided query.
        /// </summary>
        /// <param name="query">Recived query in string format.</param>
        /// <param name="handler">Qirty handler that situable for that query.</param>
        /// <returns></returns>
        public static bool TryFindQueryHandler(string query, out IQueryHandler handler)
        {
            // Detect query parts.
            QueryPart[] queryParts = DetectQueryParts(query);

            // Search.
            return TryFindQueryHandler(queryParts, out handler);
        }

        /// <summary>
        /// Looking for query handler.
        /// </summary>
        /// <param name="queryParts">Recived query splited by parts.</param>
        /// <param name="handler">Hadler that's situated to this query.</param>
        /// <returns></returns>
        public static bool TryFindQueryHandler(QueryPart[] queryParts, out IQueryHandler handler)
        {
            foreach (IQueryHandler pb in UniformQueries.API.QueryHandlers)
            {
                // Check header
                if (pb.IsTarget(queryParts))
                {
                    handler = pb;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        /// <summary>
        /// Try to detect core query parts.
        /// Example case of using: is decryption required.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsSeemsValid(string query)
        {
            // Check does contain query.
            if (query.Contains("q="))
                return true;

            return false;
        }
    }
}
