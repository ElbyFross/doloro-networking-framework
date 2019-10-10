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
using System.Xml.Serialization;

namespace UniformQueries
{
    /// <summary>
    /// Formated query part descriptor.
    /// </summary>
    [System.Serializable]
    public struct QueryPart
    {
        /// <summary>
        /// Key for access
        /// </summary>
        public string propertyName;

        /// <summary>
        /// Property that will be shared via query.
        /// </summary>
        public byte[] propertyValue;

        /// <summary>
        /// Encoding of string parts.
        /// </summary>
        public Encoding encoding;

        /// <summary>
        /// Oparate the ptoperty value like string.
        /// </summary>
        public string PropertyValueString
        {
            get
            {
                try
                {
                    if(_PropertyValueString == null)
                    {
                        _PropertyValueString = encoding.GetString(propertyValue);

                        if(_PropertyValueString.Contains("\0"))
                        {
                            _PropertyValueString = "binary";
                        }
                    }
                    return _PropertyValueString;
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    propertyValue = encoding.GetBytes(value);
                }
                else
                {
                    propertyValue = null;
                }
            }
        }

        /// <summary>
        /// Buffer that contain encoded value.
        /// </summary>
        private string _PropertyValueString;

        /// <summary>
        /// If this struct not initialized.
        /// </summary>
        [XmlIgnore]
        public bool IsNone
        {
            get
            {
                if (string.IsNullOrEmpty(propertyName))
                    return true;

                if (propertyValue == null || propertyValue.Length == 0)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Default querie part that not contains valid data.
        /// </summary>
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
            _PropertyValueString = null;
            encoding = Encoding.UTF8;
            this.propertyName = key;
            this.propertyValue = null;
        }

        /// <summary>
        /// Base constructor.
        /// Value will be null
        /// </summary>
        /// <param name="key">String key that allow to find this part in query.</param>
        /// <param name="obj">Object that woulb be converted to binary array.</param>
        public QueryPart(string key, object obj)
        {
            _PropertyValueString = null;
            encoding = Encoding.UTF8;
            this.propertyName = key;
            this.propertyValue = obj != null ? UniformDataOperator.Binary.BinaryHandler.ToByteArray(obj) : null;
        }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="key">String key that allow to find this part in query.</param>
        /// <param name="property">String property that will be available to find by key.</param>
        public QueryPart(string key, string property)
        {
            _PropertyValueString = null;
            encoding = Encoding.UTF8;
            propertyName = key;
            propertyValue = null;
            PropertyValueString = property;
        }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="key">String key that allow to find this part in query.</param>
        /// <param name="property">Binary property that will be available to find by key.</param>
        public QueryPart(string key, byte[] property)
        {
            _PropertyValueString = null;
            encoding = Encoding.UTF8;
            this.propertyName = key;
            this.propertyValue = property;
        }

        /// <summary>
        /// Return part in query format.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (propertyValue != null)
            {
                return propertyName + (PropertyValueString != null ? "=" + PropertyValueString : ":binary");
            }
            else
            {
                return propertyName + ":none";
            }
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
                    Encoding.UTF8.GetBytes(buildedPart.Substring(valueIndex+1))); // Value part
            }
            else
            {
                // Create marker query part that can be used by external parsers like instruction.
                // Examples: !prop, $prop, etc.
                return new QueryPart(buildedPart);
            }
        }

        /// <summary>
        /// Check does this query's key equals to target.
        /// </summary>
        /// <param name="key">Key for comparing.</param>
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
        /// <returns>Result of comparation.</returns>
        public bool ParamValueEqual(string param)
        {
            return string.Equals(PropertyValueString, param);
        }

        /// <summary>
        /// Check does this query's parameter equals to target.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool ParamValueEqual(object param)
        {
            return this.propertyValue.Equals(param);
        }       

        /// <summary>
        /// Try to get domain to backward connection by entry query.
        /// </summary>
        /// <param name="query">Query that was reciverd from client.</param>
        /// <param name="domain">Domain that will return in case if build is possible.</param>
        /// <returns></returns>
        public static bool TryGetBackwardDomain(Query query, out string domain)
        {
            domain = null;

            // Get query GUID.
            if (!query.TryGetParamValue("guid", out QueryPart guid)) return false;

            // Get client token.
            if (!query.TryGetParamValue("token", out QueryPart token)) return false;

            // Build domain.
            domain = guid.PropertyValueString.GetHashCode() + "_" + token.PropertyValueString.GetHashCode();

            return true;
        }

        /// <summary>
        /// Clearing cashed data. Use if you change core settings and need to recomputing.
        /// </summary>
        public void ClearCach()
        {
            _PropertyValueString = null;
        }
    }
}
