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

namespace AuthorityController.API
{
    /// <summary>
    /// Provide API to work with collection situable to authority control.
    /// </summary>
    public static class Collections
    {
        /// <summary>
        /// Compare two arrays that contain rights code.
        /// Prefix '!' before rquired right will work like "not contain this right."
        /// </summary>
        /// <param name="providedRights">Rights that provided to user.</param>
        /// <param name="requiredRights">Rights that required to get permisssion.</param>
        /// <returns></returns>
        public static bool IsHasEnoughRigths(string[] providedRights, params string[] requiredRights)
        {
            // Check every requeted and provided rights.
            bool[] operationAllowenceMask = new bool[requiredRights.Length];
            for (int i = 0; i < operationAllowenceMask.Length; i++)
            {
                // Get right that requested.
                string requiredRight = requiredRights[i];

                #region Modifiers
                // Get modifiers.
                char prefix = requiredRight[0];

                // Check non prefix.
                bool non = prefix == '!';

                // Check increse\decrease posfix
                bool higher = prefix == '>';
                bool lower = prefix == '<';

                // Remove modifier from string for comparing.
                if (non || higher || lower)
                {
                    // Exclude non prefix.
                    requiredRight = requiredRight.Substring(1);
                }
                #endregion

                // Compare with every right provided to token.
                foreach (string providedRight in providedRights)
                {
                    #region Value compare
                    if (higher || lower)
                    {
                        // Try to operate data.
                        try
                        {
                            // Value that required.
                            int requiredValue = Int32.Parse(requiredRight.Split('=')[1]);
                            // Value that provided to user.
                            int providedValue = Int32.Parse(providedRight.Split('=')[1]);

                            // Compare.
                            if (higher)
                            {
                                operationAllowenceMask[i] = providedValue > requiredValue;
                            }
                            else
                            {
                                operationAllowenceMask[i] = providedValue < requiredValue;
                            }

                            // Stop search for this right cause found.
                            break;
                        }
                        catch
                        {
                            // Mark as failed.
                            operationAllowenceMask[i] = false;
                            // Stop search for this right cause found.
                            break;
                        }
                    }
                    #endregion

                    #region Normal compare
                    // Compare provided with target.
                    if (providedRight.Equals(requiredRight))
                    {
                        // Set result.
                        // Required: true
                        // Non: false
                        operationAllowenceMask[i] = !non;
                        // Stop search for this right cause found.
                        break;
                    }
                    #endregion
                }

                // If requsted right not found via provided.
                if (!operationAllowenceMask[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loging for propery value by requested name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value">Value that stored to that property by using "PROP_NAME=PROP_VALUE" format. 
        /// NULL if empty.</param>
        /// <param name="properties">Array that contain properties' collection.</param>
        /// <returns>Result of the seeking.</returns>
        public static bool TryGetPropertyValue(string name, out string value, params string[] properties)
        {
            value = null;
            string[] parts = new string[2];

            // Check every right.
            foreach (string right in properties)
            {
                // Continue incorrct property.
                if (!right.StartsWith(name))
                {
                    continue;
                }

                // Try to detect value.
                int valueIndex = right.IndexOf('=');
                if (valueIndex != -1)
                {
                    // Get prop name
                    parts[0] = right.Substring(0, valueIndex);

                    // Skip if property name just seems similar to request.
                    if (!parts[0].Equals(name))
                    {
                        continue;
                    }

                    // Get value.
                    parts[1] = right.Substring(valueIndex + 1);

                    // Set value to ourput.
                    value = parts[1];

                    // Iform about success.
                    return true;
                }
                // If property not contain value.
                else
                {
                    // Skip if property name just seems similar to request.
                    if (!right.Equals(name))
                    {
                        continue;
                    }

                    // Iform about success.
                    return true;
                }
            }

            // Inform that property not found.
            return false;
        }
    }
}
