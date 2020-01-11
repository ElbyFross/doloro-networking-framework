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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace PipesProvider.Networking.Routing
{
    /// <summary>
    /// A descriptor of a network map that contains routes 
    /// to some servers with defined rules when each routes should be used.
    /// </summary>
    [Serializable]
    public class RoutingTable
    {
        #region Fields & properties
        /// <summary>
        /// List that contain routing instructions.
        /// </summary>
        public List<Instruction> intructions = new List<Instruction>();

        /// <summary>
        /// Path from was loaded tis table.
        /// </summary>
        [XmlIgnore]
        public string SourcePath { get; set; }
        #endregion


        #region API
        /// <summary>
        /// Trying to find target routing instruction.
        /// </summary>
        /// <param name="query">
        /// A query that will used as source for comparing with 
        /// a routing rules defined with <see cref="Instruction.queryPatterns"/> field.
        /// </param>
        /// <param name="instruction">
        /// A found instruction. 
        /// Returns the <see cref="Instruction.Empty"/> isntance in case if not found.
        /// </param>
        /// <returns>A result of the search.</returns>
        public bool TryGetRoutingInstruction(UniformQueries.Query query, out Instruction instruction)
        {
            // Allocate memory.
            bool ckeckResult;
            foreach(Instruction instBufer in intructions)
            {
                // Check if the target.
                ckeckResult = instBufer.IsRoutingTarget(query);

                // if target.
                if(ckeckResult)
                {
                    // Send to output.
                    instruction = instBufer;
                    return true;
                }
            }

            // Inform about fail.
            instruction = Instruction.Empty;
            return false;
        }
        #endregion

        #region Serialization
        /// <summary>
        /// Tries to load all routing tables from a directory.
        /// You could have several XML serialized routing tables. This way allows to share it via plugins.
        /// </summary>
        /// <param name="directory">A root folder.</param>
        /// <param name="searchOption">Definds does tne search will applied to child folders.</param>
        /// <returns>A found table. Null if not found.</returns>
        public static RoutingTable LoadRoutingTables(string directory, SearchOption searchOption)
        {
            // Create new empty table.
            RoutingTable table = new RoutingTable();

            // Check directory exist.
            if (!Directory.Exists(directory))
            {
                // Create if not found.
                Directory.CreateDirectory(directory);
            }

            // Detect all xml files in directory.
            string[] xmlFiles = Directory.GetFiles(directory, "*.xml", searchOption);

            // Init encoder.
            XmlSerializer xmlSer = new XmlSerializer(typeof(RoutingTable), Instruction.DerivedTypes);

            // Deserialize every file to table if possible.
            foreach (string fileDir in xmlFiles)
            {
                // Open stream to XML file.
                using (FileStream fs = new FileStream(fileDir, FileMode.Open))
                {
                    RoutingTable tableBufer = null;
                    try
                    {
                        // Try to deserialize routing table from file.
                        tableBufer = xmlSer.Deserialize(fs) as RoutingTable;

                        // Buferize directory to backward access.
                        tableBufer.SourcePath = fileDir;

                        // Add to  common table.
                        table += tableBufer;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("ROUTING TABLE ERROR: File reading failed. Reason:\n{0}\n", ex.Message);
                    }

                }
            }

            return table;
        }

        /// <summary>
        /// Saves a routing table by the default directory 
        /// %app/resources/routing/ROUTING.xml
        /// </summary>
        /// <param name="table">A source with instructions.</param>
        public static void SaveRoutingTable(RoutingTable table)
        {
            SaveRoutingTable(table, "resources/routing", "ROUTING");
        }


        /// <summary>
        /// Saves a routing table by the default directory
        /// %app/resources/routing
        /// </summary>
        /// <param name="table">A source with instructions.</param>
        /// <param name="name">A name of the file.</param>
        public static void SaveRoutingTable(RoutingTable table, string name)
        {
            SaveRoutingTable(table, "resources/routing", name);
        }

        /// <summary>
        /// Saves a routing table by directory.
        /// </summary>
        /// <param name="table">A source with instructions.</param>
        /// <param name="directory">A target directory.</param>
        /// <param name="name">A name of the file.</param>
        public static void SaveRoutingTable(RoutingTable table, string directory, string name)
        {
            #region Validate directory
            // Avoid null reference exception.
            if (directory == null)
            {
                directory = "";
            }

            // Check directory exist.
            if (directory != "" &&
                !Directory.Exists(directory))
            {
                // Create if not found.
                Directory.CreateDirectory(directory);
            }
            #endregion
            
            #region Write table to XML file.
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(RoutingTable), Instruction.DerivedTypes);
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, table);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(directory + "/" + name + ".xml");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ROUTING TABLE ERROR: Not serialized. Reason:\n{0}", ex.Message);
            }
            #endregion
        }
        #endregion

        #region Operators
        /// <summary>
        /// Adding instruction from second table to first one.
        /// </summary>
        /// <param name="table0"></param>
        /// <param name="table1"></param>
        /// <returns></returns>
        public static RoutingTable operator + (RoutingTable table0, RoutingTable table1)
        {
            // Validate first table.
            if (table0 == null)
            {
                // init new table.
                table0 = new RoutingTable();
            }

            // Validate second table.
            if (table1 == null)
            {
                // Operation not possible. Return first table.
                return table0;
            }

            // Find every instruction in loaded table.
            foreach (Instruction instruction in table1.intructions)
            {
                // Copy instructions to output table.
                table0.intructions.Add(instruction);
            }

            return table0;
        }
        #endregion
    }
}
