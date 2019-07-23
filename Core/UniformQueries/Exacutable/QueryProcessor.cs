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

namespace UniformQueries
{
    /// <summary>
    /// Object that provide base methods\fields\properties that allow to standartize and controll query processing.
    /// </summary>
    public abstract class QueryProcessor
    {
        #region Events
        /// <summary>
        /// Event that would be called when reciving operation would be finished.
        /// 
        /// QueryProcessor - reference to this processor.
        /// bool - result od operation
        /// object - object shared by processor.
        /// </summary>
        public event Action<QueryProcessor, bool, object> ProcessingFinished;
        #endregion

        #region Properties
        /// <summary>
        /// Does last auth's task was terminated.
        /// </summary>
        public bool IsTerminated { get; protected set; }

        /// <summary>
        /// Is authrentification in proggress.
        /// </summary>
        public bool IsInProgress { get; protected set; }
        #endregion

        /// <summary>
        /// Terminating current started process.
        /// </summary>
        public virtual void TerminateAuthorizationTask()
        {
            // Activate termination flag.
            IsTerminated = true;

            // Get time to processing.
            Thread.Sleep(50);
        }

        /// <summary>
        /// Generate ProcessingFinished event with provided params.
        /// </summary>
        /// <param name="result">Resdult of processing.</param>
        /// <param name="message">Shared message.</param>
        protected void Finalize(bool result, object args)
        {
            // Inform subscribers.
            ProcessingFinished?.Invoke(this, result, args);
        }

        /// <summary>
        /// Handler that would recive server answer.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="answer"></param>
        protected abstract void ServerAnswerHandler(object controller, object answer);
    }
}
