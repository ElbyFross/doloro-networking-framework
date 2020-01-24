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

namespace PipesProvider.Security.Encryption.Operators
{
    /// <summary>
    /// An attribute that defines what a type of the algortihm used at an encryption oeprator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EncryptionOperatorTypeAttribute : Attribute
    {
        /// <summary>
        /// A type of an operator.
        /// </summary>
        public EncryptionOperatorType OperatorType { get; private set; }

        /// <summary>
        /// Declaring an attribute with a defined type of the encryption oeprator.
        /// </summary>
        /// <param name="type"></param>
        public EncryptionOperatorTypeAttribute(EncryptionOperatorType type)
        {
            OperatorType = type;
        }
    }
}
