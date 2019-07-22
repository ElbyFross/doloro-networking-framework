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
using AuthorityController.Data.Application;

namespace AuthorityController.API
{
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
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsExpired(string token)
        {
            // Convert token to bytes array.
            byte[] data = Convert.FromBase64String(token);

            // Get when token created. Date time will take the first bytes that contain data stamp.
            DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));

            // Compare with allowed token time.
            if (when < DateTime.UtcNow.AddMinutes(-Config.Active.TokenValidTimeMinutes))
            {
                // Confirm expiration.
                return true;
            }

            // Conclude that token is valid.
            return false;
        }

        #region Rights API
        /// <summary>
        /// Check does this token has all requested rights.
        /// If token is not registred on this server then will throw UnauthorizedAccessException.
        /// </summary>
        /// <param name="token">Unitque token of the user.</param>
        /// <param name="error">Error that describe a reasone of fail. Could be send backward to client.</param>
        /// <param name="requesterRights">Rights detected to that token.</param>
        /// <param name="requiredRights">Array that contain the rights that need to by existed.</param>
        /// <returns></returns>
        public static bool IsHasEnoughRigths(
            string token,
            out string[] requesterRights, 
            out string error, 
            params string[] requiredRights)
        {
            // Initialize outputs
            requesterRights = null;
            error = null;

            try
            {
                // Check if the base rights exist.
                if (!API.Tokens.IsHasEnoughRigths(token, out requesterRights,
                    requiredRights))
                {
                    // Inform that token not registred.
                    error = "ERROR 401: Unauthorized";
                    return false;
                }
            }
            catch (Exception ex)
            {
                // if token not registred.
                if (ex is UnauthorizedAccessException)
                {
                    // Inform that token not registred.
                    error = "ERROR 401: Invalid token";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check does this token has all requested rights.
        /// If token is not registred on this server then will throw UnauthorizedAccessException.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="requiredRights"></param>
        /// <param name="requesterRights">Rights detected to that token.</param>
        /// <returns></returns>
        public static bool IsHasEnoughRigths(string token, out string[] requesterRights, params string[] requiredRights)
        {
            // Try to get token rights.
            if (!Session.Current.TryGetTokenRights(token, out requesterRights))
            {
                // Create unathorized exception.
                throw new UnauthorizedAccessException("Token not registred in the table.");
            }

            // Compare arrays.
            return API.Collections.IsHasEnoughRigths(requesterRights, requiredRights);
        }

        /// <summary>
        /// Authorizing new token with guest's rights, and return information in query format.
        /// </summary>
        /// <returns>Token that can be used by client in queries.</returns>
        public static string AuthorizeNewGuestToken()
        {
            // Get free token.
            string sessionToken = API.Tokens.UnusedToken;

            // Registrate token with guest rank.
            Session.Current.SetTokenRightsLocal(sessionToken, new string[] { "rank=0" });

            // Return session data to user.
            string query = string.Format("token={1}{0}expiryIn={2}{0}rights=rank=0",
                UniformQueries.API.SPLITTING_SYMBOL,
                sessionToken,
                Config.Active.TokenValidTimeMinutes);

            return query;
        }
        #endregion
    }
}
