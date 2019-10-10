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
using PipesProvider.Networking;

namespace PipesProvider.Client
{
    /// <summary>
    /// Privide invormation about query.
    /// </summary>
    public struct QueryContainer
    {
        #region Public properties
        /// <summary>
        /// Query that will be shared.
        /// </summary>
        public UniformQueries.Query Data { get; set; }

        /// <summary>
        /// Validate container.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (Data == null || 
                    Data.Content == null || 
                    Data.Content.Length == 0)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// How many attemts was applied to this query processing.
        /// </summary>
        public int Attempts { get; private set; }        

        /// <summary>
        /// Delegate that will be called when anser transmition will be recived.
        /// </summary>
        public Action<TransmissionLine, byte[]> AnswerHandler;
        #endregion


        #region Constructors
        /// <summary>
        /// Return empty contaier.
        /// </summary>
        public static QueryContainer Empty { get; } = new QueryContainer();

        /// <summary>
        /// Constructor that provide single way query.
        /// </summary>
        /// <param name="query">Formated query</param>
        public QueryContainer(UniformQueries.Query query)
        {
            this.Data = query;
            this.AnswerHandler = null;
            this.Attempts = 0;
        }

        /// <summary>
        /// Constructor of contaier that provide ability to create duplex query.
        /// </summary>
        /// <param name="query">Formated query.</param>
        /// <param name="AnswerHandler">Delegate that would handle answer received from server.</param>
        public QueryContainer(UniformQueries.Query query, Action<TransmissionLine, byte[]> AnswerHandler)
        {
            this.Data = query;
            this.AnswerHandler = AnswerHandler;
            this.Attempts = 0;
        }
        #endregion


        #region API
        /// <summary>
        /// Return copy of source container.
        /// </summary>
        /// <param name="source">Container that contains formated query and meta data about handler.</param>
        /// <returns>Compied container.</returns>
        public static QueryContainer Copy(QueryContainer source)
        {
            return new QueryContainer(
                source.Data.Clone() as UniformQueries.Query,
                source.AnswerHandler != null ? source.AnswerHandler.Clone() as System.Action<TransmissionLine, byte[]> : null);

        }
        
        /// <summary>
        /// Convert object to string for\mat.
        /// </summary>
        /// <returns>Returns Query property.</returns>
        public override string ToString()
        {
            return Data.ToString();
        }
        #endregion


        #region Operators
        /// <summary>
        /// Incremet of attempts count.
        /// </summary>
        /// <param name="contaier"></param>
        /// <returns></returns>
        public static QueryContainer operator ++(QueryContainer contaier)
        {
            contaier.Attempts++;
            return contaier;
        }

        /// <summary>
        /// Convert container to string.
        /// </summary>
        /// <param name="container"></param>
        public static explicit operator string(QueryContainer container)
        {
            return container.Data.ToString();
        }
        #endregion
    }
}
