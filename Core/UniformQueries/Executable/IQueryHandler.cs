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

namespace UniformQueries.Executable
{
    /// <summary>
    /// All classes that implements this interface 
    /// will automaticly detected by QueriesAPI via first use and connected to queries processing.
    /// </summary>
    public interface IQueryHandler
    {
        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        void Execute(QueryPart[] queryParts);

        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        /// <returns>Result of comparation.</returns>
        bool IsTarget(QueryPart[] queryParts);

        /// <summary>
        /// Return the description relative to the lenguage code or default if not found.
        /// </summary>
        /// <param name="cultureKey">Key of target culture.</param>
        /// <returns>Description for relative culture.</returns>
        string Description(string cultureKey);
    }
}
