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
using System.Threading;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;
using AC_API = AuthorityController.API;
using UniformQueries;

namespace ACTests.Helpers
{
    /// <summary>
    /// Class that provide possibility to create users pool.
    /// </summary>
    public static class Users
    {
        public static User user_SuperAdmin = null;
        public static User user_Admin = null;
        public static User user_Moderator = null;
        public static User user_PrivilegedUser = null;
        public static User user_User = null;
        public static User user_Guest = null;

        /// <summary>
        /// Creating and apply base users pool:
        /// -Super admin
        /// -Admin
        /// -Moderator
        /// -Privileged user
        /// -User
        /// -Guest
        /// </summary>
        public static void SetBaseUsersPool()
        {
            lock (Locks.CONFIG_LOCK)
            {
                // Set new test directory to avoid conflicts with users profiles.
                Config.Active.UsersStorageDirectory = "Tests\\Queries\\Users\\" + Guid.NewGuid().ToString() + "\\";

                // Clear current user pool.
                AC_API.LocalUsers.ClearUsersLoadedData();

                #region Create superadmin
                user_SuperAdmin = new User()
                {
                    id = 1,
                    login = "sadmin",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=16",
                    "banhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_SuperAdmin.id = AC_API.LocalUsers.GenerateID(user_SuperAdmin);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_SuperAdmin, Config.Active.UsersStorageDirectory);

                #endregion

                #region Create admin
                user_Admin = new User()
                {
                    login = "admin",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=8",
                    "banhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_Admin.id = AC_API.LocalUsers.GenerateID(user_Admin);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_Admin, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create moderator
                user_Moderator = new User()
                {
                    login = "moderator",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=4",
                    "banhammer",
                    "passwordManaging" }
                };

                // Generate ID.
                user_Moderator.id = AC_API.LocalUsers.GenerateID(user_Moderator);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_Moderator, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create privileged user
                user_PrivilegedUser = new User()
                {
                    login = "puser",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=2",
                    "passwordManaging" }
                };

                // Generate ID.
                user_PrivilegedUser.id = AC_API.LocalUsers.GenerateID(user_PrivilegedUser);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_PrivilegedUser, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create user
                user_User = new User()
                {
                    login = "user",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=1",
                    "passwordManaging" }
                };

                // Generate ID.
                user_User.id = AC_API.LocalUsers.GenerateID(user_User);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_User, Config.Active.UsersStorageDirectory);
                #endregion

                #region Create guest
                user_Guest = new User()
                {
                    login = "guest",
                    password = SaltContainer.GetHashedPassword("password", Config.Active.Salt),
                    tokens = new System.Collections.Generic.List<string>
                    (new string[] { UniformQueries.Tokens.UnusedToken }),
                    rights = new string[]{
                    "rank=0"}
                };

                // Generate ID.
                user_Guest.id = AC_API.LocalUsers.GenerateID(user_Guest);

                // Save profile.
                AC_API.LocalUsers.SetProfileAsync(user_Guest, Config.Active.UsersStorageDirectory);
                #endregion

                // Wait until loading.
                do
                {
                    Thread.Sleep(5);
                }
                while (AC_API.LocalUsers.HasAsyncLoadings);

                #region Authorize tokens
                // Super admin
                AuthorityController.Session.Current.AsignTokenToUser(user_SuperAdmin, user_SuperAdmin.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_SuperAdmin.tokens[0], user_SuperAdmin.rights);

                // Admin
                AuthorityController.Session.Current.AsignTokenToUser(user_Admin, user_Admin.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Admin.tokens[0], user_Admin.rights);

                // Moderator
                AuthorityController.Session.Current.AsignTokenToUser(user_Moderator, user_Moderator.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Moderator.tokens[0], user_Moderator.rights);

                // Privileged user
                AuthorityController.Session.Current.AsignTokenToUser(user_PrivilegedUser, user_PrivilegedUser.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_PrivilegedUser.tokens[0], user_PrivilegedUser.rights);

                // User
                AuthorityController.Session.Current.AsignTokenToUser(user_User, user_User.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_User.tokens[0], user_User.rights);

                // Guest
                AuthorityController.Session.Current.AsignTokenToUser(user_Guest, user_Guest.tokens[0]);
                AuthorityController.Session.Current.SetTokenRights(user_Guest.tokens[0], user_Guest.rights);
                #endregion
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="login">User's login.</param>
        /// <param name="password">User's password.</param>
        /// <param name="fn">User's name</param>
        /// <param name="ln">User's last name</param>
        /// <returns></returns>
        public static QueryPart[] NewUserQuery(string login, string password = "1234567!Qwerty", string fn = "FirstName", string ln = "SecondName")
        {
            return new QueryPart[]
            {
                // TODO FAKE TOKEN
                new QueryPart("token", UniformQueries.Tokens.UnusedToken),
                new QueryPart("guid", Guid.NewGuid().ToString()),

                new QueryPart("user"),
                new QueryPart("new"),

                new QueryPart("login", login),
                new QueryPart("password", password),
                new QueryPart("fn", fn),
                new QueryPart("ln", ln),

                new QueryPart("os", Environment.OSVersion.VersionString),
                new QueryPart("mac", PipesProvider.Networking.Info.MacAdsress),
                new QueryPart("stamp", DateTime.Now.ToBinary().ToString()),
            };
        }
    }
}
