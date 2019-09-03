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

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniformQueries;
using UniformQueries.Executable;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.Queries
{
    /// <summary>
    /// Create new user.
    /// 
    /// Storing profile in local dile system by default via UsersLocal API.
    /// Storing profile to SQL server in case if `UniformDataOperator.Sql.SqlOperatorHandler.Active` not null.
    /// </summary>
    public class USER_NEW : IQueryHandler
    {
        public virtual string Description(string cultureKey)
        {
            
            switch (cultureKey)
            {
                case "en-US":
                default:
                    return "USER NEW\n" +
                            "\tDESCRIPTION: Request creating of new user.\n" +
                            "\tQUERY FORMAT: user=XMLSetializedUser" + UniformQueries.API.SPLITTING_SYMBOL +
                            "new\n";
            }
        }

        public virtual void Execute(QueryPart[] queryParts)
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
               login.propertyValue.Length < Config.Active.LoginMinSize ||
               login.propertyValue.Length > Config.Active.LoginMaxSize)
            {
                // Inform about incorrect login size.
                UniformServer.BaseServer.SendAnswerViaPP(
                    "ERROR 401: Invalid login size. Require " +
                    Config.Active.LoginMinSize + "-" +
                    Config.Active.LoginMaxSize + " caracters.",
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
            if (API.LocalUsers.TryToFindUser(login.propertyValue, out User _))
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
            User userProfile = new User()
            {
                login = login.propertyValue,
                password = SaltContainer.GetHashedPassword(password.propertyValue, Config.Active.Salt),
                firstName = firstName,
                lastName = secondName
            };
            
            // Set rights default rights.
            userProfile.rights = Config.Active.UserDefaultRights;
            #endregion

            #region Data storing
            // Store in SQL data base if provided.
            if (UniformDataOperator.Sql.SqlOperatorHandler.Active != null)
            {
                // Set ignorable value to activate autoincrement.
                userProfile.id = 0;

                Task registrationTask = new Task(
                    delegate ()
                    {
                        // Set data ro data base.
                        UniformDataOperator.Sql.SqlOperatorHandler.Active.
                            SetToTable<User>(
                            userProfile, out error);

                        // Success.
                        if(string.IsNullOrEmpty(error))
                        {
                            Logon();
                        }
                        // Fail.
                        else
                        {
                            // Send answer with operation's error.
                            UniformServer.BaseServer.SendAnswerViaPP(
                                "failed:" + error,
                                queryParts);
                        }
                    },
                    Session.Current.TerminationToken);
            }
            // Store in local file system.
            else
            {
                // Provide ID.
                userProfile.id = API.LocalUsers.GenerateID(userProfile);

                // Save profile in storage.
                API.LocalUsers.SetProfileAsync(userProfile, Config.Active.UsersStorageDirectory);
                API.LocalUsers.UserProfileStored += DataStoredCallback;
                API.LocalUsers.UserProfileNotStored += DataStroringFailed;
            }
            #endregion

            #region Local callbacks
            // Callback that would be processed in case of success of data storing.
            void DataStoredCallback(User target)
            {
                // Check is that user is a target of this request.
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.LocalUsers.UserProfileStored -= DataStoredCallback;
                    API.LocalUsers.UserProfileNotStored -= DataStroringFailed;

                    Logon();
                }
            }


            // Callback that would be processed in case of fail of data storing.
            void DataStroringFailed(User target, string operationError)
            {
                if (target.id == userProfile.id)
                {
                    // Unsubscribe.
                    API.LocalUsers.UserProfileStored -= DataStoredCallback;
                    API.LocalUsers.UserProfileNotStored -= DataStroringFailed;

                    // Send answer with operation's error.
                    UniformServer.BaseServer.SendAnswerViaPP(
                        "failed:" + operationError,
                        queryParts);
                }
            }
            #endregion

            #region Localc methods
            // Request logon with current input data.
            void Logon()
            {
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
                foreach (IQueryHandler processor in UniformQueries.API.QueryHandlers)
                {
                    // Fini logon query processor.
                    if (processor is USER_LOGON)
                    {
                        // Execute and send to client token valided to created user.
                        processor.Execute(logonQuery);
                    }
                }
            }
            #endregion
        }

        public virtual bool IsTarget(QueryPart[] queryParts)
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
