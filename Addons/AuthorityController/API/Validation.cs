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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AuthorityController.API
{
    public static class Validation
    {
        /// <summary>
        /// Validate password before converting to salted hash.
        /// </summary>
        /// <param name="password">Open password.</param>
        /// <param name="error">Error string that will be situable in case of validation fail.</param>
        /// <returns>Result of validation.</returns>
        public static bool PasswordFormat(string password, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(password) ||
               password.Length < Data.Config.Active.PasswordMinAllowedLength ||
               password.Length > Data.Config.Active.PasswordMaxAllowedLength)
            {
                // Inform about incorrect login size.
                error =
                    "ERROR 401: Invalid password size. Require " +
                    Data.Config.Active.PasswordMinAllowedLength + "-" +
                    Data.Config.Active.PasswordMaxAllowedLength + " caracters.";
                return false;
            }

            // Validate format
            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9@!#$%_]+$"))
            {
                // Inform about incorrect login size.
                error = "ERROR 401: Invalid password format. Allowed symbols: [a-z][A-Z][0-9]@!#$%_";
                return false;
            }

            // Special symbol required.
            if (Data.Config.Active.PasswordRequireDigitSymbol)
            {
                if (!Regex.IsMatch(password, @"^\S*[0-9]+\S*$"))
                {
                    // Inform about incorrect login size.
                    error = "ERROR 401: Invalid password format. Need to have at least one digit 0-9";
                    return false;
                }
            }

            // Special symbol required.
            if (Data.Config.Active.PasswordRequireSpecialSymbol)
            {
                if (!Regex.IsMatch(password, @"^\S*[@!#$%_]+\S*$"))
                {
                    // Inform about incorrect login size.
                    error = "ERROR 401: Invalid password format. Need to have at least one of followed symbols: @!#$%_";
                    return false;
                }
            }

            // Upper cse required.
            if (Data.Config.Active.PasswordRequireUpperSymbol)
            {
                if (!Regex.IsMatch(password, @"^\S*[A-Z]+\S*$"))
                {
                    // Inform about incorrect login size.
                    error = "ERROR 401: Invalid password format. Need to have at least one symbol in upper case.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate name part format.
        /// </summary>
        /// <param name="namePart">First\second\middle part of the name that need to be validated</param>
        /// <param name="error">Error string that will be situable in case of validation fail.</param>
        /// <returns>Result of validation.</returns>
        public static bool NameFormat(ref string namePart, out string error)
        {
            // Remove spaces.
            namePart = namePart.Trim();

            // Validate name part (first\second\middle).
            if (!Regex.IsMatch(namePart, Data.Config.Active.UserNameRegexPattern))
            {
                // Inform about incorrect login size.
                error = "ERROR 401: Invalid name format.";
                return false;
            }
            
            error = null;
            return true;
        }
    }
}
