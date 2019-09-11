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

namespace ACTests.Tests
{
    /// <summary>
    /// Tests that confirms compatibility with UniformDataOperator.
    /// </summary>
    [TestClass]
    public class UDO
    {
        /// <summary>
        /// Set default UDO settigns relative to that tests.
        /// </summary>
        public static void SetDefaults()
        {
            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active =
                UniformDataOperator.Sql.MySql.MySqlDataOperator.Active;

            UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.UserId = Local.username;
            UniformDataOperator.Sql.MySql.MySqlDataOperator.Active.Password = Local.password;
        }

        /// <summary>
        /// Checking creating new user in database.
        /// </summary>
        [TestMethod]
        public void NewUser()
        {
            // Establish operator.
            SetDefaults();

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }

        /// <summary>
        /// Ckeckin logon of user storate in database.
        /// </summary>
        [TestMethod]
        public void UserLogon()
        {
            // Establish operator.
            SetDefaults();

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }

        /// <summary>
        /// Trying to ban user in database.
        /// </summary>
        [TestMethod]
        public void UserBan()
        {
            // Establish operator.
            SetDefaults();

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }
               
        /// <summary>
        /// Trying to change password for user in database.
        /// </summary>
        [TestMethod]
        public void UserSetPassword()
        {
            // Establish operator.
            SetDefaults();

            // Drop operator.
            UniformDataOperator.Sql.SqlOperatorHandler.Active = null;
        }
    }
}
