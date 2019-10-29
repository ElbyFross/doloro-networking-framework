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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AuthorityController.Data.Personal;
using AuthorityController.Data.Application;

namespace AuthorityController.API
{
    /// <summary>
    /// API that provide operation with authority data stored on local machine out of UniformDataOperator storage.
    /// </summary>
    public static class LocalUsers
    {
        #region Events
        /// <summary>
        /// Event that will be called when loading of users from directory will be finished.
        /// Int - count of loaded files.
        /// Int - count of corupted files.
        /// </summary>
        public static event System.Action<string, int, int> DirectoryLoadingFinished;

        /// <summary>
        /// Event that will be called when profile will be setted to storage.
        /// </summary>
        public static event System.Action<User> UserProfileStored;

        /// <summary>
        /// Event that will be called when profile will be fail adding to storage.
        /// </summary>
        public static event System.Action<User, string> UserProfileNotStored;
        #endregion

        #region Public properties
        /// <summary>
        /// Does async processes started at the moment?
        /// </summary>
        public static bool HasAsyncLoadings
        {
            get
            {
                return LoadingLockedDirectories.Count > 0 ? true : false;
            }
        }
        #endregion

        #region Private fields
        /// <summary>
        /// Table that provide aaccess to user data by login.
        /// </summary>
        private static readonly Hashtable UsersByLogin = new Hashtable();

        /// <summary>
        /// Table that provide access to user by unique ID.
        /// </summary>
        private static readonly Hashtable UsersById = new Hashtable();

        /// <summary>
        /// Contains directories that has users loading process and blocked for new ones.
        /// </summary>
        private static readonly HashSet<string> LoadingLockedDirectories = new HashSet<string>();
        #endregion


        #region Data
        /// <summary>
        /// Loading users data from directory.
        /// </summary>
        /// <param name="directory"></param>
        public static async void LoadProfilesAsync(string directory)
        {
            // Validate directory.
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("ERROR (ACAPI 0): USERS LOADING NOT POSSIBLE. DIRECTORY NOT FOUND.");

                // Inform subscribers about finish.
                DirectoryLoadingFinished?.Invoke(directory, 0, 0);

                return;
            }

            // Block if certain directory already in loading process.
            if (LoadingLockedDirectories.Contains(directory))
            {
                Console.WriteLine("ERROR (ACAPI 10): Directory alredy has active loading process. Wait until finish previous one. ({0})",
                    directory);

                // Inform subscribers about finish.
                DirectoryLoadingFinished?.Invoke(directory, 0, 0);

                return;
            }

            // Lock directory.
            LoadingLockedDirectories.Add(directory);

            // Detect files in provided directory.
            string[] xmlFiles = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);

            int loadingSucceed = 0;
            int loadingFailed = 0;

            // Running async task with loading of every profile.
            await Task.Run(() =>
            {
                // Init encoder.
                XmlSerializer xmlSer = new XmlSerializer(typeof(User));

                // Deserialize every file to table if possible.
                foreach (string fileDir in xmlFiles)
                {
                    // Open stream to XML file.
                    using (FileStream fs = new FileStream(fileDir, FileMode.Open))
                    {
                        User loadedUser = null;
                        try
                        {
                            // Try to deserialize routing table from file.
                            loadedUser = xmlSer.Deserialize(fs) as User;

                            // Add user to tables.
                            AddToLoadedData(loadedUser);

                            // Up counter.
                            loadingSucceed++;
                        }
                        catch (Exception ex)
                        {
                            // Up counter.
                            loadingFailed++;

                            // Inform.
                            Console.WriteLine("ERROR(ACAPI 20): Profile damaged. Reason:\n{0}\n", ex.Message);
                        }
                    }

                    // Share quant to other processes.
                    Thread.Yield();
                }

                // Remove directory from blocklist.
                LoadingLockedDirectories.Remove(directory);

                // Inform subscribers about location unlock.
                DirectoryLoadingFinished?.Invoke(directory, loadingSucceed, loadingFailed);
            },
            Session.Current.TerminationTokenSource.Token);
        }

        /// <summary>
        /// Adding\updating user's profile by directory sete up via config file.
        /// </summary>
        /// <param name="user">User profile.</param>
        public static void SetProfile(User user)
        {
            SetProfile(user, Config.Active.UsersStorageDirectory);
        }

        /// <summary>
        /// Adding\updating user's profile by directory.
        /// </summary>
        /// <param name="user">User profile.</param>
        /// <param name="directory">Users storage.</param>
        public static async void SetProfileAsync(User user, string directory)
        {                       
            await Task.Run(() =>
                {
                    // Lock thread not saved hashset
                    lock (LoadingLockedDirectories)
                    {
                        // Create file path.
                        string filePath = directory + GetUserFileName(user);

                        // Lock directory.
                        LoadingLockedDirectories.Add(filePath);

                        // Set profile synchronically.
                        bool result = SetProfile(user, directory);

                        // Unlock directory.
                        LoadingLockedDirectories.Remove(filePath);

                        // Inform subscribers about location unlock.
                        DirectoryLoadingFinished?.Invoke(directory, result ? 1 : 0, result ? 0 : 1);
                    }
                },
                Session.Current.TerminationTokenSource.Token);
        }

        /// <summary>
        /// Adding\updating user's profile by directory.
        /// </summary>
        /// <param name="user">User profile.</param>
        /// <param name="directory">Users storage.</param>
        public static bool SetProfile(User user, string directory)
        {
            // Update user in tables.
            AddToLoadedData(user);

            #region Save by directory
            // Check directory exist.
            if (!Directory.Exists(directory))
            {
                // Create if not found.
                Directory.CreateDirectory(directory);
            }

            string filePath = directory + GetUserFileName(user);

            // Convert user to XML file.
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(User));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, user);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(filePath);
                }

                // inform subscribers.
                UserProfileStored?.Invoke(user);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR(ACAPI 30):  Not serialized. Reason:\n{0}", ex.Message);

                // inform subscribers.
                UserProfileNotStored?.Invoke(user, ex.Message);

                return false;
            }
            #endregion
        }

        /// <summary>
        /// Remove user profile from directory seted up via Config file.
        /// </summary>
        /// <param name="user"></param>
        public static bool RemoveProfile(User user)
        {
            return RemoveProfile(user, Config.Active.UsersStorageDirectory);
        }

        /// <summary>
        /// Remove user profile from directory.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="directory"></param>
        public static bool RemoveProfile(User user, string directory)
        {
            // Expire user sessions.
            foreach (string token in user.tokens)
            {
                Session.Current.SetExpired(token);
            }

            // Remove profile.
            try
            {
                File.Delete(directory + GetUserFileName(user));
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR(ACAPI 60):  Prifile removing failed. Reason:\n{0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Looking for free id.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static uint GenerateID(User user)
        {
            // Generate ID by hash code.
            uint id = (uint)Math.Abs(user.login.GetHashCode());

            // If already exist.
            if (TryToFindUser(id, out User _))
            {
                // Increment id until finde free one.
                do
                {
                    id++;
                }
                while (TryToFindUser(id, out User _));
            }

            return id;
        }
        #endregion

        #region Cash
        /// <summary>
        /// Registrate user in tables by id and login.
        /// </summary>
        /// <param name="user"></param>
        public static void AddToLoadedData(User user)
        {
            #region Add user to ids table.
            if (UsersById[user.id] is User)
            {
                // Override if already exist.
                UsersById[user.id] = user;
            }
            else
            {
                // Add as new.
                UsersById.Add(user.id, user);
            }
            #endregion

            #region Add user to logins table.
            if (UsersByLogin[user.login] is User)
            {
                // Override if already exist.
                UsersByLogin[user.login] = user;
            }
            else
            {
                // Add as new.
                UsersByLogin.Add(user.login, user);
            }
            #endregion
        }

        /// <summary>
        /// Remove all loaded users data.
        /// </summary>
        public static void ClearUsersLoadedData()
        {
            UsersById.Clear();
            UsersByLogin.Clear();
        }

        /// <summary>
        /// Try to find user by ID in loaded users table.
        /// </summary>
        /// <param name="id">Unique user's id.</param>
        /// <param name="user">Reference on loaded user profile.</param>
        /// <returns>Result of operation.</returns>
        public static bool TryToFindUser(uint id, out User user)
        {
            // Try to find user in table.
            if (UsersById[id] is User bufer)
            {
                user = bufer;
                return true;
            }

            // Inform about fail.
            user = null;
            return false;
        }

        /// <summary>
        /// Try to find user by ID in loaded users table.
        /// </summary>
        /// <param name="login">Unique user's login.</param>
        /// <param name="user">Reference on loaded user profile.</param>
        /// <returns>Result of operation.</returns>
        public static bool TryToFindUser(string login, out User user)
        {
            try
            {
                // Try to find user in table.
                if (UsersByLogin[login] is User bufer)
                {
                    user = bufer;
                    return true;
                }
            }
            catch { }

            // Inform about fail.
            user = null;
            return false;
        }

        /// <summary>
        /// Seeking for user.
        /// </summary>
        /// <param name="uniformValue">ID or login in string format.</param>
        /// <param name="userProfile">Field that will contain user's profile in case of found.</param>
        /// <param name="error">Error that describe a reasone of fail. Could be send backward to client.</param>
        /// <returns></returns>
        public static bool TryToFindUserUniform(string uniformValue, out User userProfile, out string error)
        {
            // Initialize outputs.
            userProfile = null;
            error = null;
            // Seeking marker.
            bool userFound = false;

            // Try to parse id from query.
            if (uint.TryParse(uniformValue, out uint userId))
            {
                // Try to find user by id.
                if (TryToFindUser(userId, out userProfile))
                {
                    userFound = true;
                }
            }

            // if user not found by ID.
            if (!userFound)
            {
                // Try to find user by login.
                if (!TryToFindUser(uniformValue, out userProfile))
                {
                    // If also not found.
                    error = "ERROR 404: User not found";
                    return false;
                }
            }

            return true;
        }
        #endregion
        
        #region Private methods
        /// <summary>
        /// Return unified name based on user's profile.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetUserFileName(User user)
        {
            if (user == null)
            {
                Console.WriteLine("ERROR(ACAPI 40): User can't be null");
                throw new NullReferenceException();
            }

            // Get user ID in string format.
            string name = user.id.ToString() + ".user";
            return name;
        }
        #endregion
    }
}
