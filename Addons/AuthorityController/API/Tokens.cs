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
    /// <summary>
    /// Provides API for work with security tokens.
    /// </summary>
    public class Tokens
    {   
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
        /// <param name="_">Droped param not relative to this broadcasting.</param>
        /// <returns>Token that can be used by client in queries.</returns>
        public static byte[] AuthorizeNewGuestToken(
            PipesProvider.Server.TransmissionControllers.BroadcastingServerTransmissionController _)
        {
            return AuthorizeNewGuestToken();
        }

        /// <summary>
        /// Authorizing new token with guest's rights, and return information in query format.
        /// </summary>
        /// <returns>Query in binary format that contain token's data.</returns>
        public static byte[] AuthorizeNewGuestToken()
        {
            // Get free token.
            string sessionToken = UniformQueries.Tokens.UnusedToken;

            // Registrate token with guest rank.
            Session.Current.SetTokenRights(sessionToken, new string[] { "rank=0" });

            // Buiding query.
            UniformQueries.Query query = new UniformQueries.Query(
                new UniformQueries.QueryPart("token", sessionToken),
                new UniformQueries.QueryPart("expiryIn", DateTime.UtcNow.AddMinutes(Config.Active.TokenValidTimeMinutes)),
                new UniformQueries.QueryPart("rights", "rank=0")
                );

            return UniformDataOperator.Binary.BinaryHandler.ToByteArray(query);
        }
        #endregion
    }
}
