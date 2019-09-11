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
using System.Data;
using System.Collections.Generic;
using AuthorityController.Data.Application;
using UniformDataOperator.Sql.Attributes;
using UniformDataOperator.Sql.MySql.Attributes;
using MySql.Data.MySqlClient;

namespace AuthorityController.Data.Personal
{
    /// <summary>
    /// Object that contain relevant data about user.
    /// </summary>
    [System.Serializable]
    [Table("DNFAuthControl", "user", "InnoDB")]
    public partial class User
    {
        /// <summary>
        /// Type of user. Apply your castom user to this field to instinate required one.
        /// </summary>
        public static Type GlobalType = typeof(User);

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
        [Column("login", DbType.String), IsNotNull]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string login;

        /// <summary>
        /// ON CLIENT: Open password. Situable only if user provides profile as new.
        /// ON SERVER: Hashed and salted password that confirm user rights to use this account.
        /// </summary>
        [Column("password", DbType.Binary), IsNotNull]
        [MySqlDBTypeOverride(MySqlDbType.Blob, "BLOB(512)")]
        public byte[] password;

        /// <summary>
        /// Name of the user that will displayed in profile.
        /// </summary>
        [Column("firstname", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string firstName;

        /// <summary>
        /// Last name that will be displayed in profile.
        /// </summary>
        [Column("lastname", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(45)")]
        public string lastName;

        /// <summary>
        /// Rights array as formated string.
        /// Compatible with UDO.
        /// </summary>
        [Column("rights", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(1000)")]
        public string RightsStringFormat
        {
            get
            {
                string result = "";
                foreach(string s in rights)
                {
                    if(string.IsNullOrEmpty(result))
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
        /// Array of rigts' codes provided to this user.
        /// </summary>
        public string[] rights = new string[0];
        
        /// <summary>
        /// List of bans that would received by user.
        /// </summary>
        public List<BanInformation> bans = new List<BanInformation>();
        #endregion

        #region Session-time fields
        /// <summary>
        /// List that cont tokens provided to this user.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public List<string> tokens = new List<string>();
        #endregion


        #region API
        /// <summary>
        /// Compare recived open password with stored to user.
        /// </summary>
        /// <param name="recivedPassword"></param>
        /// <returns></returns>
        public bool IsOpenPasswordCorrect(string recivedPassword)
        {
            // Get hashed password from recived.
            byte[] recivedHashedPassword = SaltContainer.GetHashedPassword(recivedPassword, Config.Active.Salt);

            // Compare.
            return IsHashedPasswordCorrect(recivedHashedPassword);
        }

        /// <summary>
        /// Check does the recived password is the same as stored to user.
        /// </summary>
        /// <param name="recivedHashedPassword"></param>
        /// <returns></returns>
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
