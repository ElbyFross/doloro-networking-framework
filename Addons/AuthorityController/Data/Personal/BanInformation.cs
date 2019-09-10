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
using UniformDataOperator.Sql.Tables.Attributes;
using UniformDataOperator.Sql.MySql.Attributes;
using System.Xml.Serialization;

namespace AuthorityController.Data.Personal
{
    /// <summary>
    /// Provide information about user bans.
    /// </summary>
    [System.Serializable]
    [Table("DNFAuthControl", "bans", "InnoDB")]
    public class BanInformation
    {
        /// <summary>
        /// Ban's duration mode.
        /// </summary>
        public enum Duration
        {
            Temporary,
            Permanent
        }

        /// <summary>
        /// Return information for permanent ban.
        /// </summary>
        public static BanInformation Permanent
        {
            get
            {
                return new BanInformation()
                {
                    active = true,
                    duration = Duration.Permanent,
                    commentary = "Not described."
                };
            }
        }

        #region Public fields and properties
        /// <summary>
        /// Unique identifier of this ban.
        /// </summary>
        [Column("banid", System.Data.DbType.Int32), IsNotNull, IsPrimaryKey, IsAutoIncrement]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.Int32, "INT")]
        public int id = -1;

        /// <summary>
        /// If of user that reived that ban.
        /// </summary>
        [Column("user_userid", System.Data.DbType.Int32), IsNotNull, IsForeignKey("DNFAuthControl", "user", "userid")]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.Int32, "INT")]
        public int userId = -1;

        /// <summary>
        /// Marker that make
        /// </summary>
        [Column("acive", System.Data.DbType.Boolean)]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.Bit, "TINYINT(1)")]
        public bool active;

        /// <summary>
        /// Duration in int format that can be stored to the SQL server.
        /// </summary>
        [Column("duration", System.Data.DbType.Int32)]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.Int32, "INT")]
        [XmlIgnore]
        public int DurationInt
        {
            get
            {
                return (int)duration;
            }
            set
            {
                duration = (Duration)value;
            }
        }

        /// <summary>
        /// Duration mode of this ban.
        /// 
        /// Permanent will no have expiry time.
        /// </summary>
        public Duration duration;

        /// <summary>
        /// Date Time when this bun will be expired.
        /// </summary>
        [Column("expiryTime", System.Data.DbType.String)]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.VarChar, "VARCHAR(256)")]
        public DateTime expiryTime;

        /// <summary>
        /// Resones for ban.
        /// </summary>
        [Column("commentary", System.Data.DbType.String)]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.VarChar, "VARCHAR(256)")]
        public string commentary;

        /// <summary>
        /// Blocked rights array in binnary format.
        /// </summary>
        [Column("blockedRights", System.Data.DbType.Binary)]
        [MySqlDBTypeOverride(MySql.Data.MySqlClient.MySqlDbType.TinyBlob, "TNYBLOB")]
        [XmlIgnore]
        public byte[] BlockedRightsBlob
        {
            get
            {
                return blockedRights == null ? null : UniformDataOperator.Binary.BinaryHandler.ToByteArray<string[]>(blockedRights);
            }
            set
            {
                if (value != null)
                {
                    UniformDataOperator.Binary.BinaryHandler.FromByteArray<string[]>(value);
                }
            }
        }

        /// <summary>
        /// Rights that was blocked for this user.
        /// 
        /// Recommend:
        /// logon - block possibility to logon.
        /// commenting - block possibility to post commentaries.
        /// etc.
        /// </summary>
        public string[] blockedRights;
        #endregion

        #region API
        /// <summary>
        /// Check is this ban still actual.
        /// </summary>
        /// <returns></returns>
        public bool IsExpired
        {
            get
            {
                // Check for line key expiring.
                bool isExpired = DateTime.Compare(expiryTime, DateTime.Now) < 0;
                if (isExpired)
                {
                    // Mark as invalid if expired.
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Check permition for action.
        /// </summary>
        /// <param name="user">Target user.</param>
        /// <param name="rightCode">Code of right that required for action.</param>
        /// <returns></returns>
        public static bool IsBanned(User user, string rightCode)
        {
            // Check every ban.
            for (int i = 0; i < user.bans.Count; i++)
            {
                // Get ban data.
                BanInformation banInformation = user.bans[i];

                // Skip if ban expired.
                if (!banInformation.active)
                    continue;

                // Validate ban and disable it if already expired.
                if (banInformation.IsExpired)
                {
                    // Disactivate ban.
                    banInformation.active = false;

                    // Update profile.
                    API.LocalUsers.SetProfileAsync(user, Application.Config.Active.UsersStorageDirectory);

                    // Skip cause already expired.
                    continue;
                }

                // Check every baned right.
                foreach (string blockedRights in banInformation.blockedRights)
                {
                    // Compare rights codes.
                    if (blockedRights == rightCode)
                    {
                        // Confirm band if equal.
                        return true;
                    }
                }
            }

            // ban not found.
            return false;
        }

        /// <summary>
        /// Recieving data from connected SQL server based on user profile meta.
        /// </summary>
        /// <param name="user">Profile that contain core meta like id, login, etc.</param>
        /// <returns></returns>
        public static async Task RecieveServerDataAsync(User user, System.Action<BanInformation> callback)
        {
            // Init new ben info. 
            BanInformation banInformation = new BanInformation();

            // Subscribe on sql error events.
            UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured += SQLErrorListener;
            
            // Request data from server.
            await UniformDataOperator.Sql.SqlOperatorHandler.Active.SetToObjectAsync(
                typeof(BanInformation), 
                Session.Current.TerminationToken, 
                banInformation,
                new string[0],
                new string[]
                {
                    "user_userid"
                });


            void SQLErrorListener(object sender, string message)
            {
                // Is event target.
                if(!banInformation.Equals(sender))
                {
                    return;
                }

                // Unsubscribe from event.
                UniformDataOperator.Sql.SqlOperatorHandler.SqlErrorOccured -= SQLErrorListener;
            }
        }
        #endregion
    }
}
