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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthorityController.Data.Application;

namespace ACTests.Tests
{
    [TestClass]
    public class Regex
    {
        /// <summary>
        /// Set configs related to base password test.
        /// </summary>
        private void SetBasePasswordConfig()
        {
            new Config()
            {
                PasswordRequireUpperSymbol = false,
                PasswordRequireDigitSymbol = false,
                PasswordRequireSpecialSymbol = false,
                PasswordMinAllowedLength = 5,
                PasswordMaxAllowedLength = 16
            };
        }

        /// <summary>
        /// Set configs related to base personal data test.
        /// </summary>
        private void SetBasePersonalDataConfig()
        {
            new Config()
            {
                
            };
        }

        /// <summary>
        /// Validate name in format
        /// Name-name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType1()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Volodymyr";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType2()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "DeGole";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }


        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType3()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Al'Said";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType4()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Anna Grace";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_InvalidType1()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "anna";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(!result, error);
            }
        }
        
        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_InvalidType2()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Ben4";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_InvalidType3()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "A";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_InvalidType4()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Jorge!";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(!result, error);
            }
        }
        
        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_InvalidType5()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Jorge-";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType5()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = "Anna-Sofia";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Validate name in format
        /// Name'name
        /// </summary>
        [TestMethod]
        public void ComplexNameValidation_ValidType6()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePersonalDataConfig();

                string name = " Jorge";

                // Validate
                bool result = AuthorityController.API.Validation.NameFormat(ref name, out string error);

                Assert.IsTrue(result, error);
            }
        }


        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseValid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Valid base password.
                string passwordValid1 = "qwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordValid1, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseValid2()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Valid base password.
                string passwordValid2 = "qw_22erty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordValid2, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseValid3()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Valid base password.
                string passwordValid3 = "AdsqASSAD";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordValid3, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseInvalid1()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Too short.
                string passwordInvalid1 = "qw";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid1, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseInvalid2()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Incorrect language.
                string passwordInvalid2 = "йц";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid2, out string error);

                Assert.IsTrue(!result, error);
            }

        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseInvalid3()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Incorrect symbols.
                string passwordInvalid3 = ", .";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid3, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Check base password type.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_BaseInvalid4()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                // Create default config file.
                SetBasePasswordConfig();

                // Too long.
                string passwordInvalid4 = "qwertyqwertyqwertyqwertyqwertyqwertyqwertyqwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled upper case required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_UpperCaseValid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireUpperSymbol = true;

                // Valid one
                string passwordInvalid4 = "Qwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled upper case required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_UpperCaseInvalid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireUpperSymbol = true;

                // invalid one
                string passwordInvalid4 = "qwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled special symbol required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_SpecialSymbolValid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireSpecialSymbol = true;

                // invalid one
                string passwordInvalid4 = "qwerty!";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled special symbol required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_SpecialSymbolInvalid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireSpecialSymbol = true;

                // invalid one
                string passwordInvalid4 = "qwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(!result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled digits required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_DigitsValid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireDigitSymbol = true;

                // invalid one
                string passwordInvalid4 = "qwerty4";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(result, error);
            }
        }

        /// <summary>
        /// Check filter when enabled digits required.
        /// </summary>
        [TestMethod]
        public void ValidatePassord_DigitsInvalid()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                SetBasePasswordConfig();
                Config.Active.PasswordRequireDigitSymbol = true;

                // invalid one
                string passwordInvalid4 = "qwerty";

                // Validate
                bool result = AuthorityController.API.Validation.PasswordFormat(passwordInvalid4, out string error);

                Assert.IsTrue(!result, error);
            }
        }
    }
}
