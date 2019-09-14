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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace AuthorityController.Data
{
    /// <summary>
    /// Provide metods that simplifying work with data.
    /// </summary>
    public static class Handler
    {
        #region Local
        /// <summary>
        /// Trying to serialize obkject to XML format.
        /// </summary>
        /// <param name="data">Object to serizlization.</param>
        /// <param name="xml">Object in string format.</param>
        /// <returns></returns>
        public static bool TryXMLSerialize<T>(T data, out string xml)
        {
            // Convert table to XML file.
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StringWriter stream = new StringWriter())
                {
                    serializer.Serialize(stream, data);
                    xml = stream.ToString();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\"{1}\" SERIALIZATION ERROR:\n{0}", ex.Message, typeof(T).Name);
                xml = null;
                return false;
            }
        }

        /// <summary>
        /// Trying to convert XML string to object instance.
        /// </summary>
        /// <param name="xml">XML string format of object.</param>
        /// <param name="data">Instiniated object.</param>
        /// <returns>Does convertation passed success.</returns>
        public static bool TryXMLDeserizlize<T>(string xml, out T data)
        {
            // Convert table to XML file.
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StringReader stream = new StringReader(xml))
                {
                    data = (T)serializer.Deserialize(stream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\"{1}\" DESERIALIZATION ERROR:\n{0}", ex.Message, typeof(T).Name);
                data = default;
                return false;
            }
        }
        #endregion

        #region File system
        /// <summary>
        /// Saving config file to directory.
        /// </summary>
        /// <param name="obj">Object that contains data.</param>
        /// <param name="directory">Target folder directory.</param>
        /// <param name="fileName">Name of the file that would created \ rewrited.</param>
        public static void SaveAs<T>(object obj, string directory, string fileName)
        {
            // Check directory exist.
            if (!Directory.Exists(directory))
            {
                // Create new if not exist.
                Directory.CreateDirectory(directory);
            }

            // Convert table to XML file.
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, obj);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(directory + fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Auth control error (ACC 10): Not serialized. Reason:\n{0}", ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Trying to deserialize object from XML file.
        /// </summary>
        /// <typeparam name="T">Required type</typeparam>
        /// <param name="path">Full path to file.</param>
        /// <param name="result">Deserizlised object.</param>
        /// <returns></returns>
        public static bool TryToLoad<T>(string path, out T result)
        {
            // Check file exist.
            if (!File.Exists(path))
            {
                result = default;
                return false;
            }

            // Init encoder.
            XmlSerializer xmlSer = new XmlSerializer(typeof(T));

            // Open stream to XML file.
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                try
                {
                    // Try to deserialize object from file.
                    result = (T)xmlSer.Deserialize(fs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Auth control error (ACC 20): File reading failed. Reason:\n{0}\n", ex.Message);
                    result = default;
                    return false;
                }
            }

            return true;

        }
        #endregion
    }
}
