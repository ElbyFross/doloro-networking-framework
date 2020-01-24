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

namespace UniformQueries.Executable.Security
{
    /// <summary>
    /// Provides API situated for handling authentication queries.
    /// </summary>
    public abstract class AuthQueryProcessor : QueryProcessor
    {
        #region Properties
        /// <summary>
        /// Check does this instruction is authorized.
        /// </summary>
        public bool IsAutorized
        {
            get
            {
                if (string.IsNullOrEmpty(Token))
                {
                    return false;
                }

                return _isAutorized;
            }
            protected set
            {
                _isAutorized = value;
            }
        }

        /// <summary>
        /// A loken that would be used during quries to confirm the rights.
        /// Logon on a target server before using an instruction and save and recived token to this property.
        /// </summary>
        public string Token
        {
            get
            {
                return _token;
            }
            set
            {
                // Set value.
                _token = value;

                // Mark as authrized.
                IsAutorized = true;
            }
        }

        /// <summary>
        /// A time when token would expited.
        /// </summary>
        public System.DateTime ExpiryTime { get; protected set; }

        /// <summary>
        /// A rights provided to token during logon.
        /// </summary>
        public string[] RecivedRights { get; protected set; }
        #endregion

        #region Private fields
        private bool _isAutorized;
        private string _token;
        #endregion

        /// <summary>
        /// A handler that would recive server answer.
        /// </summary>
        /// <param name="_">A dropped param that not required on that processor.</param>
        /// <param name="answer">A binary data received from server as answer.</param>
        protected override void ServerAnswerHandler(object _, object answer)
        {
            // Trying to convert answer to string
            if (answer is Query query)
            {
                // Buferizing received query.
                _ServerAnswer = query;

                // Get token.
                if (query.TryGetParamValue("token", out QueryPart tokenBufer))
                {
                    // Apply token.
                    Token = tokenBufer.PropertyValueString;

                    // Set marker.
                    IsAutorized = true;

                    // Get expiry time.
                    if (query.TryGetParamValue("expiryIn", out QueryPart expiryIn))
                    {
                        // Decode expiry time from binary format.
                        ExpiryTime = System.DateTime.FromBinary(
                            UniformDataOperator.Binary.BinaryHandler.FromByteArray<long>(expiryIn.propertyValue));
                    }

                    // Get provided rights.
                    if (query.TryGetParamValue("rights", out QueryPart rights))
                    {
                        // Get rights.
                        RecivedRights = rights.PropertyValueString.Split('+');
                    }

                    // Inform subscribers.
                    Finalize(true, Token);
                }
                else
                {
                    // Inform about fail.
                    Finalize(false, query.First);
                }
            }
            else
            {
                // Inform about fail.
                Finalize(false, "ERROR: Incorrect answer format.");
            }

            // Disable in progress marker.
            IsInProgress = false;
        }
    }
}
