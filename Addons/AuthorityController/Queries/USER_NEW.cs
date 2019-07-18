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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniformQueries;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Create new user.
    /// </summary>
    public class USER_NEW : IQueryHandler
    {
        public string Description(string cultureKey)
        {
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER NEW\n" +
                            "\tDESCRIPTION: Request new password for user." +
                            "\n\tToken confirm rights to change it.\n" +
                            "\n\tOld password required to avoid access from public places.\n" +
                            "\tQUERY FORMAT: user=XMLSetializedUser" + UniformQueries.API.SPLITTING_SYMBOL +
                            "new\n";
            }
        }

        public void Execute(QueryPart[] queryParts)
        {
            #region Get qyery params
            UniformQueries.API.TryGetParamValue("login", out QueryPart login, queryParts);
            UniformQueries.API.TryGetParamValue("password", out QueryPart password, queryParts);
            UniformQueries.API.TryGetParamValue("fn", out QueryPart firstName, queryParts);
            UniformQueries.API.TryGetParamValue("sn", out QueryPart secondName, queryParts);

            UniformQueries.API.TryGetParamValue("token", out QueryPart token, queryParts);
            UniformQueries.API.TryGetParamValue("guid", out QueryPart guid, queryParts);
            UniformQueries.API.TryGetParamValue("os", out QueryPart os, queryParts);
            UniformQueries.API.TryGetParamValue("mac", out QueryPart mac, queryParts);
            UniformQueries.API.TryGetParamValue("stamp", out QueryPart timeStamp, queryParts);

            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate login
            if (string.IsNullOrEmpty(login.propertyValue) ||
               login.propertyValue.Length < Data.Config.Active.LoginMinSize ||
               login.propertyValue.Length > Data.Config.Active.LoginMaxSize)
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login size. Require " +
                    Data.Config.Active.LoginMinSize + "-" +
                    Data.Config.Active.LoginMaxSize + " caracters.",
                    queryParts);
                return;
            }

            // Check login format.
            if(!Regex.IsMatch(login.propertyValue, @"^[a-zA-Z0-9@._]+$"))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login format. Allowed symbols: [a-z][A-Z][0-9]@._" ,
                    queryParts);
                return;

            }

            // Check login exist.
            if (API.Users.TryToFindUser(login.propertyValue, out Data.User _))
            {
                // Inform that target user has the same or heigher rank then requester.
                UniformServer.BaseServer.SendAnswerViaPP("ERROR 401: Login occupied", queryParts);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate password
            if (!API.Validation.PasswordFormat(password.propertyValue, out string errorMessage))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    errorMessage,
                    queryParts);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion

            #region Validate names
            // Validate name.
            if(!API.Validation.NameFormat(ref firstName.propertyValue, out string error) ||
               !API.Validation.NameFormat(ref secondName.propertyValue, out error))
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    error,
                    queryParts);
                return;
            }

            // Can take enough long time so just let other query to process.
            System.Threading.Thread.Sleep(5);
            #endregion


            // There you can apply some filter of rood words.

            //-----------------------------------------------

            #region Create user profile data.
            // Create base data.
            Data.User userProfile = new Data.User()
            {
                login = login.propertyValue,
                password = API.Users.GetHashedPassword(password.propertyValue, Data.Config.Active.Salt),
                firstName = firstName,
                secondName = secondName
            };

            // Provide ID.
            userProfile.id = API.Users.GenerateID(userProfile);

            // Set rights default rights.
            userProfile.rights = Data.Config.Active.UserDefaultRights;
            #endregion

            // Save profile in storage.
            API.Users.SetProfileAsync(userProfile, Data.Config.Active.UsersStorageDirectory);
            API.Users.UserProfileStored += DataStoredCallback;
            API.Users.UserProfileNotStored += DataStroringFailed;

            // Marker that will enabled untill operation will recive result.
            bool storingInProgress = true;
            // Marker that contin result of storing operation.
            bool storingResult = false;
            // Field that would contain operation error in case of fail.
            string storingError = null;


            void DataStoredCallback(Data.User target)
            {
                // Check is that user is a target of this request.
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.Users.UserProfileStored -= DataStoredCallback;

                    // Unblock loop.
                    storingInProgress = false;

                    // Set operation result.
                    storingResult = true;
                }

            }

            void DataStroringFailed(Data.User target, string operationError)
            {
                // Check is that user is a target of this request.
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.Users.UserProfileNotStored -= DataStroringFailed;

                    // Unblock loop.
                    storingInProgress = false;

                    // Set operation result.
                    storingResult = false;
                    storingError = operationError;
                }
            }

            // Wait until operation finish.
            while (storingInProgress)
            {
                System.Threading.Thread.Sleep(5);
            }

            if (storingResult)
            {
                #region Return token to client
                // Build logon query.
                QueryPart[] logonQuery = new QueryPart[]
                    {
                    new QueryPart("USER", null),
                    new QueryPart("LOGON", null),
                    token,
                    guid,
                    login,
                    password,
                    os,
                    mac,
                    timeStamp,
                    };

                    // Create logon subquery.
                  foreach(UniformQueries.IQueryHandler processor in UniformQueries.API.QueryHandlers)
                  {
                      // Fini logon query processor.
                      if(processor is USER_LOGON)
                      {
                          // Execute and send to client token valided to created user.
                          processor.Execute(logonQuery);
                      }
                  }
                #endregion
            }
            else
            {
                // Send answer with operation's error.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "failed:" + storingError,
                    queryParts
                    );
            }
        }

        public bool IsTarget(QueryPart[] queryParts)
        {
            // Check token exist.
            if (!UniformQueries.API.QueryParamExist("token", queryParts))
                return false;

            // Check guid exist.
            if (!UniformQueries.API.QueryParamExist("guid", queryParts))
                return false;


            // USER prop.
            if (!UniformQueries.API.QueryParamExist("user", queryParts))
                return false;

            // NEW prop.
            if (!UniformQueries.API.QueryParamExist("new", queryParts))
                return false;


            if (!UniformQueries.API.QueryParamExist("login", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("password", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("fn", queryParts))
                return false;

            if (!UniformQueries.API.QueryParamExist("sn", queryParts))
                return false;


            // User operation system.
            if (!UniformQueries.API.QueryParamExist("os", queryParts))
                return false;

            // Mac adress of logon device.
            if (!UniformQueries.API.QueryParamExist("mac", queryParts))
                return false;

            // Session open time
            if (!UniformQueries.API.QueryParamExist("stamp", queryParts))
                return false;

            return true;
        }
    }
}
