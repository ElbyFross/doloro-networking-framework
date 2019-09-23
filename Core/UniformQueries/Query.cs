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
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace UniformQueries
{
    /// <summary>
    /// Formated query that can be shared in binary view cross network.
    /// </summary>
    [System.Serializable]
    public class Query : ICloneable
    {
        /// <summary>
        /// Code of encryption operator that applied to that query.
        /// </summary>
        public string encytpion;

        /// <summary>
        /// Custom configs data. Can be used to share specific meta suitable for operating with that query.
        /// </summary>
        public byte[] configs;

        /// <summary>
        /// Binary array with content. Could be encrypted.
        /// In normal state is List of QueryPart.
        /// </summary>
        public byte[] content;

        /// <summary>
        /// Returning content in listed format of possible. Null if not.
        /// Serialize listed content to binary format.
        /// </summary>
        [XmlIgnore]
        public List<QueryPart> ListedContent
        {
            get
            {
                if(listedContent == null)
                {
                    try
                    {
                        listedContent = UniformDataOperator.Binary.BinaryHandler.FromByteArray<List<QueryPart>>(content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Listed content not found. Details: " + ex.Message);
                    }
                }
                return listedContent;
            }
            set
            {
                content = UniformDataOperator.Binary.BinaryHandler.ToByteArray<List<QueryPart>>(value);
            }
        }

        /// <summary>
        /// Bufer that contains deserialized binary content.
        /// </summary>
        [XmlIgnore]
        private List<QueryPart> listedContent;
        
        /// <summary>
        /// Default consructor.
        /// </summary>
        public Query() { }

        /// <summary>
        /// Creating query with message as content.
        /// </summary>
        /// <param name="message"></param>
        [Obsolete]
        public Query(string message)
        {
            var parts = API.DetectQueryParts(message);

            listedContent = new List<QueryPart>();
            foreach (QueryPart part in parts)
            {
                listedContent.Add(part);
            }
        }

        /// <summary>
        /// Creating query from parts.
        /// </summary>
        /// <param name="parts"></param>
        public Query(params QueryPart[] parts)
        {
            listedContent = new List<QueryPart>();
            foreach(QueryPart part in parts)
            {
                listedContent.Add(part);
            }
        }

        /// <summary>
        /// Check existing of param in query parts.
        /// </summary>
        /// <param name="param">Parameter that would be looked in query.</param>
        /// <returns>Is parameter exist.</returns>
        public bool QueryParamExist(string param)
        {
            // Drop if isted content not exist.
            if (ListedContent == null) return false;

            // Try to find target param
            foreach (QueryPart part in ListedContent)
            {
                // If target param
                if (part.ParamNameEqual(param))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Try to find requested param's value among query parts.
        /// </summary>
        /// <param name="param">Parameter to search in listed content.</param>
        /// <param name="value">Query's part that found.</param>
        /// <returns>Result of search.</returns>
        public bool TryGetParamValue(string param, out QueryPart value)
        {
            // Drop if isted content not exist.
            if (ListedContent == null)
            {
                value = QueryPart.None;
                return false;
            }

            // Try to find target param
            foreach (QueryPart part in ListedContent)
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
        /// Returns copy of that object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var bufer = new Query()
            {
                encytpion = string.Copy(encytpion)
            };

            if (content != null)
            {
                Array.Copy(content, bufer.content, content.Length);
            }

            if (configs != null)
            {
                Array.Copy(configs, bufer.configs, content.Length);
            }

            return bufer;
        }

        /// <summary>
        /// Return query in string format.
        /// </summary>
        /// <returns>String formtated query.</returns>
        public override string ToString()
        {
            if (ListedContent != null)
            {
                string query = "";

                foreach(QueryPart qp in ListedContent)
                {
                    if(!string.IsNullOrEmpty(qp))
                    {
                        query += "&";
                    }
                    query += qp.ToString();
                }
                return query;
            }
            else
            {
                return "Query content is not listed.";
            }
        }
    }
}
