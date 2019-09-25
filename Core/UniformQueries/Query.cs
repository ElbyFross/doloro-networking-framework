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
        /// Setiing of qury encryption.
        /// </summary>
        [System.Serializable]
        public class EncryptionSettings : ICloneable
        {
            /// <summary>
            /// Code of encryption operator that applied to that query.
            /// </summary>
            public string encytpionOperatorCode;

            /// <summary>
            /// Is that encryption based on asym,etric keys.
            /// </summary>
            public bool asymmetricEncryption = false;

            /// <summary>
            /// Custom configs data. Can be used to share specific meta suitable for operating with that query.
            /// </summary>
            public byte[] configs;

            /// <summary>
            /// Return cloned object of settings.
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                var settigns = new EncryptionSettings()
                {
                    asymmetricEncryption = this.asymmetricEncryption,
                    encytpionOperatorCode = string.Copy(this.encytpionOperatorCode)
                };

                if (configs != null)
                {
                    Array.Copy(this.configs, configs, this.configs.Length);
                }

                return settigns;
            }
        }

        /// <summary>
        /// Encryption setting of that query.
        /// if null then messages would not encrypted.
        /// </summary>
        public EncryptionSettings Encryption { get; set; }
        
        /// <summary>
        /// Binary array with content. Could be encrypted.
        /// In normal state is List of QueryPart.
        /// </summary>
        public byte[] Content
        {
            get
            {
                if(content == null)
                {
                    if (listedContent != null)
                    {
                        _ = ListedContent; // Convert listed content ro binary format.
                    }
                }
                return content;
            }
            set
            {
                content = value;
                listedContent = null;
            }
        }

        protected byte[] content;

        /// <summary>
        /// Returns first query part or QueryPart.None if not available.
        /// </summary>
        public QueryPart First
        {
            get
            {
                if (ListedContent != null && listedContent.Count > 0)
                {
                    return listedContent[0];
                }

                return QueryPart.None;
            }
        }

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
                        listedContent = UniformDataOperator.Binary.BinaryHandler.FromByteArray<List<QueryPart>>(Content);
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
                Content = UniformDataOperator.Binary.BinaryHandler.ToByteArray<List<QueryPart>>(value);
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
        /// <param name="message">Message that would be available via `value` param.</param>
        public Query(string message)
        {
            listedContent = new List<QueryPart>
            {
                new QueryPart("value", message)
            };
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
                Encryption = Encryption != null ? (EncryptionSettings)this.Encryption.Clone() : null
            };

            if (Content != null)
            {
                Array.Copy(Content, bufer.Content, Content.Length);
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
                        query += API.SPLITTING_SYMBOL;
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
