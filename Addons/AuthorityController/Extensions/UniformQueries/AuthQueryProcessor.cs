﻿//Copyright 2019 Volodymyr Podshyvalov
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

namespace UniformQueries
{
    /// <summary>
    /// Provide fields situated but authentification queries.
    /// </summary>
    public abstract class AuthQueryProcessor : QueryProcessor
    {
        #region Properties
        /// <summary>
        /// Check does this instruction authorized.
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
        /// Token that would be used during quries to confirm the rights.
        /// Logon on target server before using this instruction and save recived token to this property.
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
        /// Time when token would expited.
        /// </summary>
        public string ExpiryTime { get; protected set; }

        /// <summary>
        /// Rights provided to token during logon.
        /// </summary>
        public string[] RecivedRights { get; protected set; }
        #endregion

        #region Private fields
        private bool _isAutorized;
        private string _token;
        #endregion

        /// <summary>
        /// Handler that would recive server answer.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="answer"></param>
        protected override void ServerAnswerHandler(object _, object answer)
        {
            // Trying to convert answer to string
            if (answer is string answerS)
            {
                // Detect parts from query.
                QueryPart[] parts = UniformQueries.API.DetectQueryParts(answerS);

                // Get token.
                if (UniformQueries.API.TryGetParamValue("token", out QueryPart tokenBufer, parts))
                {
                    // Apply token.
                    Token = tokenBufer.propertyValue;

                    // Set marker.
                    IsAutorized = true;

                    // Get expiry time.
                    if (UniformQueries.API.TryGetParamValue("expiryIn", out QueryPart expiryIn, parts))
                    {
                        // Apply expire time.
                        ExpiryTime = expiryIn.propertyValue;
                    }

                    // Get provided rights.
                    if (UniformQueries.API.TryGetParamValue("rights", out QueryPart rights, parts))
                    {
                        // Get rights.
                        RecivedRights = rights.propertyValue.Split('+');
                    }

                    // Inform subscribers.
                    Finalize(true, Token);
                }
                else
                {
                    // Inform about fail.
                    Finalize(false, "ERROR: Token no provided.");
                }
            }
            else
            {
                // Inform about fail.
                Finalize(false, "ERROR: Inccorect answer format.");
            }

            // Disable in progress marker.
            IsInProgress = false;
        }
    }
}
