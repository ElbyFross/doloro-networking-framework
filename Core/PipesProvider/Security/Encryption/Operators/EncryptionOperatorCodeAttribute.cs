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
    /// Declares an unique code for an operator source type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited =false)]
    public class EncryptionOperatorCodeAttribute : Attribute
    {
        /// <summary>
        /// An unquie code of the encryption operator.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Defines a code of an operator.
        /// </summary>
        /// <param name="code"></param>
        public EncryptionOperatorCodeAttribute(string code)
        {
            Code = code;
        }
    }
}
