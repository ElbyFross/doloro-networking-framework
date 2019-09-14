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
using System.Linq;

namespace UniformQueries
{
    /// <summary>
    /// Class that provide API for works with tokens.
    /// </summary>
    public class Tokens
    {   
        /// <summary>
        /// Return free token.
        /// </summary>
        public static string UnusedToken
        {
            get
            {
                // Get current time.
                byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
                // Generate id.
                byte[] key = Guid.NewGuid().ToByteArray();
                // Create token.
                string token = Convert.ToBase64String(time.Concat(key).ToArray());

                return token;
            }
        }

        /// <summary>
        /// Check if token expired based on encoded token data.
        /// Use it on Queries Server to avoid additive time spending on data servers and unnecessary connections.
        /// 
        /// If token have hacked allocate date this just will lead to passing of this check.
        /// Server wouldn't has has token so sequrity will not be passed.
        /// Also server will control expire time by him self.
        /// </summary>
        /// <param name="token">Token in string format.</param>
        /// <param name="expiryTime">Time when token would expired.</param>
        /// <returns></returns>
        public static bool IsExpired(string token, DateTime expiryTime)
        {
            // Convert token to bytes array.
            byte[] data = Convert.FromBase64String(token);

            // Get when token created. Date time will take the first bytes that contain data stamp.
            DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));

            // Compare with allowed token time.
            if (when < expiryTime)
            {
                // Confirm expiration.
                return true;
            }

            // Conclude that token is valid.
            return false;
        }
    }
}
