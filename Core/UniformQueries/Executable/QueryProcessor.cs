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
using System.Threading;

namespace UniformQueries.Executable
{
    /// <summary>
    /// An abstract class that provides a base methods\fields\properties those allow to standardize and control queries processing.
    /// </summary>
    public abstract class QueryProcessor
    {
        #region Events
        /// <summary>
        /// Event that will occurs when implemented operation is finished.
        /// 
        /// QueryProcessor - reference to this processor.
        /// bool - a result of operation
        /// object - an object shared by processor.
        /// </summary>
        public event Action<QueryProcessor, bool, object> ProcessingFinished;
        #endregion

        #region Properties
        /// <summary>
        /// Does a last operation is terminated.
        /// </summary>
        public bool IsTerminated { get; protected set; }

        /// <summary>
        /// Is an opertion is in proggress.
        /// </summary>
        public bool IsInProgress { get; protected set; }
        #endregion

        /// <summary>
        /// Terminating current started process.
        /// </summary>
        public virtual void Terminate()
        {
            // Activate termination flag.
            IsTerminated = true;

            // Get time to processing.
            Thread.Sleep(50);
        }

        /// <summary>
        /// A query received from a server for handling.
        /// </summary>
        /// <remarks>
        /// In case of set will simulate answer from server and will use params for shared <see cref="Query"/>. 
        /// </remarks>
        public virtual Query ServerAnswer
        {
            get { return _ServerAnswer; }
            set { ServerAnswerHandler(null, value); }
        }

        /// <summary>
        /// Bufer that contains last applied server answer.
        /// </summary>
        protected Query _ServerAnswer;

        /// <summary>
        /// Generates the <see cref="ProcessingFinished"/> event with provided params.
        /// </summary>
        /// <param name="result">Resdult of processing.</param>
        /// <param name="args">Shared object.</param>
        protected void Finalize(bool result, object args)
        {
            // Inform subscribers.
            ProcessingFinished?.Invoke(this, result, args);
        }

        /// <summary>
        /// A handler for server's answer.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="answer">Answer received from the server.</param>
        protected abstract void ServerAnswerHandler(object controller, object answer);
    }
}
