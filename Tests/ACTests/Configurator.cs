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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthorityControllerTests
{
    public class Configurator
    {
        /// <summary>
        /// Current active instqance of test configurator.
        /// </summary>
        public static Configurator Active
        {
            get
            {
                if(active == null)
                {
                    active = new Configurator();
                }
                return active;
            }
        }
        private static Configurator active;

        /// <summary>
        /// Return unique subfolder for the test.
        /// </summary>
        public static string TestSubfolder
        {
            get
            {
                if (_testSubFolder == null)
                {
                    _testSubFolder = "Tests\\" + Guid.NewGuid().ToString();

                    // Open folder.
                    Directory.CreateDirectory(_testSubFolder);
                    System.Diagnostics.Process.Start(_testSubFolder);
                }
                return _testSubFolder;
            }
        }
        private static string _testSubFolder;

        /// <summary>
        /// Marker that need to avoid tests conflicts.
        /// </summary>
        public static bool CONFIG_FILE_GENERATED = false;

        /// <summary>
        /// Marker tha would be activated when ban would activated.
        /// </summary>
        public static bool BAN_PLACED = false;

        /// <summary>
        /// Provided guest token.
        /// </summary>
        public static string GUEST_TOKEN = null;
    }
}
