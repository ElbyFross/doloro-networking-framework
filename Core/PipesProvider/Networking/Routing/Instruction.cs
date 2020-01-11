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
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using PipesProvider.Security.Encryption.Operators;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// A struct that contains an instruction about target adress by relative query params.
    /// </summary>
    /// <remarks>
    /// Allows to use of several servers via one public.
    /// 
    /// Example:
    ///                          -- Authification server
    /// Client -- Query server -- Data server 1
    ///                          -- Data server 2
    /// </remarks>
    [Serializable]
    public class Instruction
    {
        #region Public properties
        /// <summary>
        /// Configurated assymetric encryption operator that would be used during transmission.
        /// Using for sharing of small messages not longer then 117 bytes.
        /// </summary>
        [XmlIgnore]
        public IEncryptionOperator AsymmetricEncryptionOperator = new RSAEncryptionOperator();

        /// <summary>
        /// Check does loading was failed or key was expired.
        /// </summary>
        [XmlIgnore]
        public virtual bool IsValid
        {
            get
            {
                // If encryption enabled.
                if (encryption)
                {
                    // Check for line key expiring.
                    bool isExpired = DateTime.Compare(AsymmetricEncryptionOperator.ExpiryTime, DateTime.Now) < 0;
                    if (isExpired)
                    {
                        // Mark as invalid if expired.
                        return false;
                    }
                }
                return true;
            }
        }
        #endregion

        #region Public fields
        /// <summary>
        /// A title of the instruction that can be shown in applications' UI.
        /// </summary>
        public string title = "New instruction";

        /// <summary>
        /// A commentary added to this instruction.
        /// </summary>
        public string commentary = "";

        /// <summary>
        ///  A network address of the server. Set an IP or a network name of the server. 
        /// </summary>
        public string routingIP = "localhost";

        /// <summary>
        /// A name of the named pipe for access to the server .
        /// </summary>
        public string pipeName = "";

        /// <summary>
        /// A logon config that allows to impersonate a user at the remote device.
        /// </summary>
        public Security.LogonConfig logonConfig = Security.LogonConfig.Anonymous;

        /// <summary>
        /// An array that contain querie's body that need to be routed by this instruction.
        /// </summary>
        /// <remarks>
        /// Format:
        /// property=value&amp;property=value&amp;... etc.
        /// Encount all properties that need to be a part of query by splitting with UniformQueries.API.SPLITTING_SYMBOL ('&amp;' by default).
        /// !property - this property must be out of query.
        /// $property - this property must exist in query.
        /// 
        /// targetQueries[0] = "q=GET&amp;sq="PUBLICKEY";   // All queries that contain GET query and PUBLICKEY sub-query will routed.
        /// targetQueries[1] = "q=GET&amp;!pk";             // All queries that request data from server but has no RSA public keys for backward encription will wouted.
        /// targetQueries[1] = "$customProp";               // All queries that have "customProp" property in query will be routed.
        /// </remarks>
        public string[] queryPatterns = new string[] { "" };

        /// <summary>
        /// Does this chanel has encryption?
        /// If true then client will ask for server's Public Key 
        /// for safe exchange of a symmetric keys and message encryption before sending.
        /// </summary>
        public bool encryption = true;
        #endregion

        #region Static properties
        /// <summary>
        /// Returns a default instruction.
        /// </summary>
        public static Instruction Default
        {
            get
            {
                return new Instruction()
                {
                    logonConfig = PipesProvider.Security.LogonConfig.Anonymous,
                    queryPatterns = new string[] { "$guid,$token" },
                    routingIP = "localhost",
                    pipeName = "THB_DS_QM_MAIN_INOUT",
                    encryption = false
                };
            }
        }

        /// <summary>
        /// Returns an empty instruction.
        /// </summary>
        public static Instruction Empty
        {
            get { return new Instruction(); }
        }
        #endregion

        /// <summary>
        /// Tries to detectan  encryption operator by an operator's internal code.
        /// </summary>
        /// <param name="code">A code of the operator.</param>
        /// <returns>An operator that was found.</returns>
        /// <exception cref="NotSupportedException">If operator's code is invalid.</exception>
        public IEncryptionOperator FindEncryptorByCode(string code)
        {
            code = code.ToLower();

            switch (code)
            {
                case "rsa": return AsymmetricEncryptionOperator;
                default: throw new NotSupportedException("\""+ code + "\" IEncryptionOperator not exist in that instruction.");
            }
        }

        /// <summary>
        /// Check doest this query must be routed using this server instruction.
        /// </summary>
        /// <param name="query">Query received from client.</param>
        /// <returns></returns>
        public bool IsRoutingTarget(UniformQueries.Query query)
        {
            // Declere variables out of loops for avoid additive allocating.
            bool valid = true;
            char instructionOperator;

            // Check every pattern.
            foreach (string pattern in queryPatterns)
            {
                // Marker that shoved up checking up result.
                valid = true;

                // Split pattern to instructions.
                UniformQueries.QueryPart[] patternParts = UniformQueries.API.DetectQueryParts(pattern, ',');

                // Compare every instruction.
                foreach (UniformQueries.QueryPart pp in patternParts)
                {
                    // Skip damaged or empty.
                    if(string.IsNullOrEmpty(pp.propertyName))
                    {
                        continue;
                    }

                    // If instruction.
                    #region Instuction processing
                    if (string.IsNullOrEmpty(pp.PropertyValueString))
                    {
                        instructionOperator = pp.propertyName[0];

                        switch (instructionOperator)
                        {
                            // Not contain instruction.
                            case '!':
                                // Check parameter existing.
                                if (query.QueryParamExist(pp.propertyName.Substring(1)))
                                {
                                    // Mark as invalid if found.
                                    valid = false;
                                }
                                break;

                            // Property exist instruction.
                            case '$':
                                // Check parameter existing.
                                if (!query.QueryParamExist(pp.propertyName.Substring(1)))
                                {
                                    // Mark as invalid if not found.
                                    valid = false;
                                }
                                break;
                            default:
                                // Check parameter existing.
                                if (!query.QueryParamExist(pp.propertyName))
                                {
                                    // Mark as invalid if not found.
                                    valid = false;
                                }
                                break;
                        }
                    }
                    #endregion
                    // If full query part.
                    #region Query part processing
                    else
                    {
                        // Get last chat in name. If it's "!" then this request operation NOT EQUAL.
                        char nonInstruction = pp.propertyName[pp.propertyName.Length - 1];
                        bool notEqualRequested = nonInstruction.Equals('!');
                        string croppedParamName = pp.propertyName.Substring(0, pp.propertyName.Length - 1);

                        // Try to get requested value.
                        if (query.TryGetParamValue(croppedParamName, out UniformQueries.QueryPart propertyBufer))
                        {
                            // Check param value.
                            if (notEqualRequested)
                            {
                                valid = !propertyBufer.ParamValueEqual(pp.PropertyValueString);
                            }
                            else
                            {
                                valid = propertyBufer.ParamValueEqual(pp.PropertyValueString);
                            }
                        }
                        else
                        {
                            // Only if not requested instruction NON. In this case not exist will equal non.
                            if (!notEqualRequested)
                            {
                                // Mark as invalide if param not found.
                                valid = false;
                            }
                        }
                    }
                    #endregion

                    // Drop if instruction already failed.
                    if (!valid) break;
                }

                // Drop if instruction validated.
                if (valid) break;
            }

            // Return validation result.
            return valid;
        }

        /// <summary>
        /// Returns an array of Instruction's types derived from the Instruction.
        /// If you need to rescan a solution then set the value to null and call again.
        /// </summary>
        public static Type[] DerivedTypes
        {
            get
            {
                // Search for types if not found yet.
                if (_DerivedTypes == null)
                {
                    // Getting extra types suitable for custom routing instructions.
                    System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    IEnumerable<Type> deliveredTypes = new List<Type>();
                    foreach (System.Reflection.Assembly a in assemblies)
                    {
                        try
                        {
                            var subclasses = a.GetTypes().Where(type => type.IsSubclassOf(typeof(Instruction)));
                            deliveredTypes = deliveredTypes.Concat<Type>(subclasses);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ASSEMBLY ERROR: RoutingTable serialization : " + ex.Message);
                        }
                    }
                    _DerivedTypes = deliveredTypes.ToArray();
                }

                return _DerivedTypes;
            }
            set
            {
                _DerivedTypes = value;
            }
        }

        /// <summary>
        /// A cashed array with found derived types.
        /// </summary>
        private static Type[] _DerivedTypes;
    }
}
