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
        /// Data that describe required or applied encryption.
        /// </summary>
        [Serializable]
        public class EncryptionInfo : ICloneable
        {
            /// <summary>
            /// Code of encryption operator that applied to the query's content.
            /// </summary>
            public string contentEncytpionOperatorCode;

            /// <summary>
            /// Symmetric key that used for content encryption.
            /// Encrupted by public key received from server.
            /// </summary>
            public byte[] encryptedSymmetricKey;

            /// <summary>
            /// Return cloned object of settings.
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                var settigns = new EncryptionInfo()
                {
                    contentEncytpionOperatorCode = string.Copy(this.contentEncytpionOperatorCode)
                };

                if (encryptedSymmetricKey != null)
                {
                    Array.Copy(this.encryptedSymmetricKey, encryptedSymmetricKey, this.encryptedSymmetricKey.Length);
                }

                return settigns;
            }
        }

        /// <summary>
        /// Encryption setting of that query.
        /// if null then messages would not encrypted.
        /// </summary>
        public EncryptionInfo EncryptionMeta { get; set; }

        /// <summary>
        /// Is that query has configurated encryption meta?
        /// If it is then system would mean that the content was or require to be encrypted and would to operate that.
        /// </summary>
        [XmlIgnore]
        public bool IsEncrypted
        {
            get
            {
                if (EncryptionMeta == null) return false;
                if (string.IsNullOrEmpty(EncryptionMeta.contentEncytpionOperatorCode)) return false;

                return true;
            }
        }

        /// <summary>
        /// Binary array with content. Could be encrypted.
        /// In normal state is List of QueryPart.
        /// </summary>
        [XmlIgnore]
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

        /// <summary>
        /// Binary data shared via that query. Can be encrypted.
        /// </summary>
        protected byte[] content;

        /// <summary>
        /// If true than output handler will wait for receiving answer input handler and
        /// only after that will start next query in queue.
        /// </summary>
        public bool WaitForAnswer { get; set; } = false;

        /// <summary>
        /// Returns first query part or QueryPart.None if not available.
        /// </summary>
        [XmlIgnore]
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
                Content = UniformDataOperator.Binary.BinaryHandler.ToByteArray(value);
                listedContent = value;
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
            ListedContent = new List<QueryPart>
            {
                new QueryPart("value", message)
            };
        }

        /// <summary>
        /// Creating query from parts.
        /// </summary>
        /// <param name="encrypted">Is that query myst be encrypted? 
        /// Auto configurate QncryptionInfo.</param>
        /// <param name="parts">Query parts that would be used as Listed content.</param>
        public Query(bool encrypted, params QueryPart[] parts)
        {
            // Init encryption to allow auto definition.
            EncryptionMeta = new EncryptionInfo();

            // Creating listed content.
            ListedContent = new List<QueryPart>(parts);
        }

        /// <summary>
        /// Creating query from parts.
        /// </summary>
        /// <param name="meta">Encryption descriptor. Set at leas empty EncriptionInfor to 
        /// requiest auto definition of settings.</param>
        /// <param name="parts">Query parts that would be used as Listed content.</param>
        public Query(EncryptionInfo meta, params QueryPart[] parts)
        {
            // Applying encryption descriptor.
            EncryptionMeta = meta;

            // Creating listed content.
            ListedContent = new List<QueryPart>(parts);
        }

        /// <summary>
        /// Creating query from parts.
        /// </summary>
        /// <param name="parts">Query parts that would be used as Listed content.</param>
        public Query(params QueryPart[] parts)
        {
            var lc = new List<QueryPart>();
            foreach(QueryPart part in parts)
            {
                lc.Add(part);
            }

            ListedContent = lc;
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
        /// Setting part to listed content. 
        /// Update existing if found.
        /// </summary>
        /// <param name="queryPart">Target query part.</param>
        public void SetParam(QueryPart queryPart)
        {
            // Try to get data in listed format.
            var lc = ListedContent;

            // If lsted data not found.
            if (lc == null)
            {
                // Drop if has a binary format of data.
                if(content != null && content.Length > 0)
                {
                    throw new NotSupportedException("Your query contain binary data in not ListedContent compatible view." +
                        " Adding query part not possible.");
                }

                // Init listed content.
                lc = new List<QueryPart>
                {
                    queryPart
                };
                ListedContent = lc;
                return;
            }
            else
            {
                // Looking for existed property.
                for(int i = 0; i < lc.Count; i++)
                {
                    // Compare by name.
                    if(lc[i].ParamNameEqual(queryPart.propertyName))
                    {
                        lc[i] = queryPart;  // Set new data.
                        ListedContent = lc; //  Convert to binary.
                        return;
                    }
                }

                // Add as new if not found.
                lc.Add(queryPart);
                ListedContent = lc; //  Convert to binary.
                return;
            }
        }

        /// <summary>
        /// Returns copy of that object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var bufer = new Query()
            {
                EncryptionMeta = EncryptionMeta != null ? (EncryptionInfo)this.EncryptionMeta.Clone() : null,
            };

            if (Content != null)
            {
                bufer.Content = new byte[Content.Length];
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
                    if(!string.IsNullOrEmpty(query))
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

        /// <summary>
        /// Converting query to string.
        /// </summary>
        /// <param name="query"></param>
        public static implicit operator string(Query query)
        {
            return query.ToString();
        }
    }
}
