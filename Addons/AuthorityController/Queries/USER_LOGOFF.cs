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
using UniformQueries;
using UniformQueries.Executable;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Log out user.
    /// Expire shared token.
    /// </summary>
    public class USER_LOGOFF : IQueryHandler
    {
        /// <summary>
        /// Return the description relative to the lenguage code or default if not found.
        /// </summary>
        /// <param name="cultureKey">Key of target culture.</param>
        /// <returns>Description for relative culture.</returns>
        public string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER LOGOFF\n" +
                            "\tDESCRIPTION: Logoff token from system.\n" +
                            "\tQUERY FORMAT: TOKEN=tokenForLogoff + " + UniformQueries.API.SPLITTING_SYMBOL +
                            "USER" + UniformQueries.API.SPLITTING_SYMBOL + "LOGOFF\n";
            }
        }

        /// <summary>
        /// Methods that process query.
        /// </summary>
        /// <param name="sender">Operator that call that operation</param>
        /// <param name="queryParts">Recived query parts.</param>
        public void Execute(object sender, QueryPart[] queryParts)
        {
            // Get params.
            UniformQueries.API.TryGetParamValue("token", out QueryPart token, queryParts);

            // Request logoff.
            LogoffToken(token.propertyValue);
        }

        /// <summary>
        /// Request token expiring that equal to logoff operation.
        /// </summary>
        /// <param name="token"></param>
        public static bool LogoffToken(string token)
        {
            // Set expired.
            return Session.Current.SetExpired(token);
        }
        
        /// <summary>
        /// Check by the entry params does it target Query Handler.
        /// </summary>
        /// <param name="queryParts">Recived query parts.</param>
        /// <returns>Result of comparation.</returns>
        public bool IsTarget(QueryPart[] queryParts)
        {
            // Check query.
            if (!UniformQueries.API.QueryParamExist("USER", queryParts))
                return false;

            // Check query.
            if (!UniformQueries.API.QueryParamExist("LOGOFF", queryParts))
                return false;

            return true;
        }
    }
}
