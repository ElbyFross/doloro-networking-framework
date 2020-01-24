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
using System.Threading.Tasks;
using System.Linq;
using AuthorityController.Data.Application;
using AuthorityController.Data.Temporal;
using PipesProvider.Server.TransmissionControllers;

namespace AuthorityController.API
{
    /// <summary>
    /// Provides an API for work with security tokens.
    /// </summary>
    public static class Tokens
    {
        /// <summary>
        /// Checks does a token has all requested rights.
        /// </summary>
        /// <param name="token">
        /// An unique token privided to some user.
        /// </param>
        /// <param name="error">
        /// An error that describe a reasone of fail. Could be send backward to client.
        /// </param>
        /// <param name="tokenInfo">
        /// A found information about the token. 
        /// Includs rights provided to the token.
        /// </param>
        /// <param name="requiredRights">
        /// Rights required from the token to passing through.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Occurs in case if the token not registred at the server.
        /// </exception>
        /// <returns>A result of check.</returns>
        public static bool IsHasEnoughRigths(
            string token,
            out TokenInfo tokenInfo, 
            out string error, 
            params string[] requiredRights)
        {
            // Initialize outputs
            tokenInfo = null;
            error = null;

            try
            {
                // Check if the base rights exist.
                if (!IsHasEnoughRigths(token, out tokenInfo,
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
        /// Checks does a token has all requested rights.
        /// </summary>
        /// <param name="token">
        /// A token for check.
        /// </param>
        /// <param name="requiredRights">
        /// Rights required from the token to passing through.
        /// </param>
        /// <param name="tokenInfo">
        /// A found information about the token. 
        /// Includs rights provided to the token.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Occurs in case if the token not registred at the server.
        /// </exception>
        /// <returns>A result of check.</returns>
        public static bool IsHasEnoughRigths(
            string token, 
            out TokenInfo tokenInfo, 
            params string[] requiredRights)
        {
            // Try to get token rights.
            if (!Session.Current.TryGetTokenInfo(token, out tokenInfo))
            {
                // Create unathorized exception.
                throw new UnauthorizedAccessException("Token not registred in the table.");
            }

            // Compare arrays.
            return Collections.IsHasEnoughRigths(tokenInfo.rights, requiredRights);
        }

        /// <summary>
        /// Authorizes a new token with guests rights 
        /// and returns an information in a query format.
        /// 
        /// Mades `AuthorizeNewGuestToken` comatible to the <see cref="BroadcastTransmissionController"/> handler.
        /// </summary>
        /// <param name="_">Droped param not relative to this broadcasting.</param>
        /// <returns>A token that can be used by client in queries.</returns>
        public static byte[] AuthorizeNewGuestToken(BroadcastTransmissionController _)
        {
            return AuthorizeNewGuestToken();
        }

        /// <summary>
        /// Authorizes a new token with guests rights 
        /// and returns an information in a query format.
        /// </summary>
        /// <returns>
        /// A query in a binary format that contains a token's data.
        /// </returns>
        public static byte[] AuthorizeNewGuestToken()
        {
            // Get free token.
            string sessionToken = UniformQueries.Tokens.UnusedToken;

            // Registrate token with guest rank.
            Session.Current.SetTokenRights(sessionToken, new string[] { "rank=0" });

            // Buiding query.
            UniformQueries.Query query = new UniformQueries.Query(
                new UniformQueries.QueryPart("token", sessionToken),
                new UniformQueries.QueryPart("expiryIn", DateTime.UtcNow.AddMinutes(Config.Active.TokenValidTimeMinutes).ToBinary()),
                new UniformQueries.QueryPart("rights", "rank=0")
                );

            return UniformDataOperator.Binary.BinaryHandler.ToByteArray(query);
        }
    }
}
