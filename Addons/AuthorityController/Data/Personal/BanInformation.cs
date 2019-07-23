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

namespace AuthorityController.Data.Personal
{
    /// <summary>
    /// Provide information about user bans.
    /// </summary>
    [System.Serializable]
    public struct BanInformation
    {
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

        /// <summary>
        /// Marker that make
        /// </summary>
        public bool active;

        /// <summary>
        /// Duration mode of this ban.
        /// 
        /// Permanent will no have expiry time.
        /// </summary>
        public Duration duration;

        /// <summary>
        /// Date Time in binary format when this bun will be expired.
        /// </summary>
        public long expiryTime;

        /// <summary>
        /// Resones for ban.
        /// </summary>
        public string commentary;

        /// <summary>
        /// Rights that was blocked for this user.
        /// 
        /// Recommend:
        /// logon - block possibility to logon.
        /// commenting - block possibility to post commentaries.
        /// etc.
        /// </summary>
        public string[] blockedRights;

        /// <summary>
        /// Check is this ban still actual.
        /// </summary>
        /// <returns></returns>
        public bool IsExpired
        {
            get
            {
                // Check for line key expiring.
                bool isExpired = DateTime.Compare(DateTime.FromBinary(expiryTime), DateTime.Now) < 0;
                if (isExpired)
                {
                    // Mark as invalid if expired.
                    return true;
                }
                return false;
            }
        }

    }
}
