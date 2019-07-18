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

namespace UniformQueries
{
    public struct QueryPart
    {
        /// <summary>
        /// Key for access
        /// </summary>
        public string propertyName;

        /// <summary>
        /// Property that will be shared via query.
        /// </summary>
        public string propertyValue;

        /// <summary>
        /// If this struct not initialized.
        /// </summary>
        public bool IsNone
        {
            get
            {
                if (string.IsNullOrEmpty(propertyName))
                    return true;

                if (string.IsNullOrEmpty(propertyValue))
                    return true;

                return false;
            }
        }

        public static QueryPart None
        {
            get { return new QueryPart(); }
        }

        /// <summary>
        /// Base constructor.
        /// Value will be null
        /// </summary>
        /// <param name="key">String key that allow to find this part in query.</param>
        public QueryPart(string key)
        {
            this.propertyName = key;
            this.propertyValue = null;
        }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="key">String key that allow to find this part in query.</param>
        /// <param name="property">String property that will be available to  find by key.</param>
        public QueryPart(string key, string property)
        {
            this.propertyName = key;
            this.propertyValue = property;
        }

        /// <summary>
        /// Return part in query format.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return propertyName + (propertyValue != null ? "=" + propertyValue : "");
        }

        /// <summary>
        /// Convert QueryPart to string.
        /// </summary>
        /// <param name="qp"></param>
        public static implicit operator string(QueryPart qp)
        {
            return qp.ToString();
        }

        /// <summary>
        /// Convert string to Qury Part.
        /// </summary>
        /// <param name="buildedPart"></param>
        public static explicit operator QueryPart(string buildedPart)
        {
            // Drop invalid request.
            if (string.IsNullOrEmpty(buildedPart))
            {
                Console.WriteLine("QueryPart converting error: provided part is null or empty");
                return new QueryPart();
            }

            // Split string by spliter.
            int valueIndex = buildedPart.IndexOf('=');

            // If splided as require.
            if (valueIndex != -1)
            {
                return new QueryPart(
                    buildedPart.Substring(0, valueIndex), // Param part
                    buildedPart.Substring(valueIndex+1)); // Value part
            }
            else
            {
                // Create marker query part that can be used by external parsers like instruction.
                // Examples: !prop, $prop, etc.
                return new QueryPart(buildedPart, null);
            }
        }

        /// <summary>
        /// Convert array to query string.
        /// </summary>
        /// <param name="queryParts"></param>
        /// <returns></returns>
        public static string QueryPartsArrayToString(QueryPart[] queryParts)
        {
            string query = "";

            // Processing of every part.
            for (int i = 0; i < queryParts.Length; i++)
            {
                // Add query part.
                query += queryParts[i];

                // Add splitter.
                if (i < queryParts.Length - 1)
                    query += API.SPLITTING_SYMBOL;
            }

            return query;
        }
        
        /// <summary>
        /// Check does this query's key equals to target.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool ParamNameEqual(string key)
        {
            // Try to compare
            try
            {
                return this.propertyName.Equals(key, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Failed.
                return false;
            }
        }

        /// <summary>
        /// Check does this query's parameter equals to target.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool ParamValueEqual(string param)
        {
            return this.propertyValue.Equals(param, StringComparison.OrdinalIgnoreCase);
        }       

        /// <summary>
        /// Try to get domain to backward connection by entry query.
        /// </summary>
        /// <param name="queryParts">Query that was reciverd from client.</param>
        /// <param name="domain">Domain that will return in case if build is possible.</param>
        /// <returns></returns>
        public static bool TryGetBackwardDomain(QueryPart[] queryParts, out string domain)
        {
            domain = null;

            // Get query GUID.
            if (!API.TryGetParamValue("guid", out QueryPart guid, queryParts)) return false;

            // Get client token.
            if (!API.TryGetParamValue("token", out QueryPart token, queryParts)) return false;

            // Build domain.
            domain = guid.propertyValue.GetHashCode() + "_" + token.propertyValue.GetHashCode();

            return true;
        }
    }
}
