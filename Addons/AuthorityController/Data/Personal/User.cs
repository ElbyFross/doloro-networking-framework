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
using System.Xml.Serialization;
using System.Data;
using System.Collections.Generic;
using AuthorityController.Data.Application;
using UniformDataOperator.Sql.Markup;
using UniformDataOperator.Sql.MySql.Markup;
using MySql.Data.MySqlClient;

namespace AuthorityController.Data.Personal
{
    /// <summary>
    /// An object that contains a relevant data about a user.
    /// </summary>
    [Serializable]
    [Table("DNFAuthControl", "user", "InnoDB")]
    public partial class User 
    {
        #region Serialized fields
        /// <summary>
        /// Unique id of this user to allow services access.
        /// </summary>
        [Column("userid", DbType.Int32), IsPrimaryKey, IsNotNull, IsAutoIncrement(0)]
        [MySqlDBTypeOverride(MySqlDbType.Int32, "INT")]
        public uint id;

        /// <summary>
        /// Login of this user to access the system.
        /// </summary>
        [Column("login", DbType.String), IsNotNull, IsUnique]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string login;

        /// <summary>
        /// ON CLIENT: An open password. Situable only if user provides a profile as new.
        /// ON SERVER: A hashed and salted password that confirms user rights to use this account.
        /// </summary>
        [Column("password", DbType.Binary), IsNotNull]
        [MySqlDBTypeOverride(MySqlDbType.Blob, "BLOB(512)")]
        public byte[] password;

        /// <summary>
        /// A name of the user that will displayed in the profile.
        /// </summary>
        [Column("firstname", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string firstName = "";

        /// <summary>
        /// A last name that will be displayed in the profile.
        /// </summary>
        [Column("lastname", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string lastName = "";
        
        /// <summary>
        /// An array of rigts' codes provided to this user.
        /// </summary>
        public string[] rights = new string[0];
        
        /// <summary>
        /// A list of bans that would received by a user.
        /// </summary>
        public List<BanInformation> bans = new List<BanInformation>();
        #endregion

        #region Session-time fields
        /// <summary>
        /// A rights array as formated string.
        /// Compatible with the UniformDataOperator framework.
        /// </summary>
        [Column("rights", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(1000)")]
        [XmlIgnore]
        public string RightsStringFormat
        {
            get
            {
                string result = "";
                foreach (string s in rights)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += "+";
                    }
                    result += s;
                }
                return result;
            }
            set
            {
                rights = value.Split('+');
            }
        }

        /// <summary>
        /// A list that contains tokens provided to this user.
        /// </summary>
        [XmlIgnore]
        public List<string> tokens = new List<string>();
        #endregion


        #region API
        /// <summary>
        /// Compares a recived open password with a stored to an user.
        /// </summary>
        /// <param name="recivedPassword">
        /// A password received by a server in open format.
        /// Will hashed in compared with a stored one.
        /// </param>
        /// <returns>A result of comparison.</returns>
        public bool IsOpenPasswordCorrect(string recivedPassword)
        {
            // Gets a hashed password from recived one.
            byte[] recivedHashedPassword = SaltContainer.GetHashedPassword(recivedPassword, Config.Active.Salt);

            // Compares.
            return IsHashedPasswordCorrect(recivedHashedPassword);
        }

        /// <summary>
        /// Checks does the recived password is the same as a stored to an user.
        /// </summary>
        /// <param name="recivedHashedPassword">
        /// A password received by a server in open format.
        /// Will hashed in compared with a stored one.
        /// </param>
        /// <returns>A result of comparison.</returns>
        public bool IsHashedPasswordCorrect(byte[] recivedHashedPassword)
        {
            // Compare length to avoid long time comparing.
            if (password.Length != recivedHashedPassword.Length)
            {
                return false;
            }

            // Compare every byte.
            for (int i = 0; i < password.Length; i++)
            {
                if (password[i] != recivedHashedPassword[i])
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
