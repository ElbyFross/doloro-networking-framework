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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthorityController.Data;
using System.Threading;

namespace ACTests.Tests
{
    [TestClass]
    public class Data
    {
        /// <summary>
        /// Marker that need to avoid tests conflicts.
        /// </summary>
        public static bool CONFIG_FILE_GENERATED = false;

        [TestMethod]
        public void ConfigValidation()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                Config_New();
                Config_Load_ValidData();
                Config_Load_CoruptedData();
            }
        }

        /// <summary>
        /// Create new config file.
        /// </summary>
        public void Config_New()
        {
            // Set new directory.
            Config.DIRECTORY = Helpers.FileSystem.TestSubfolder + Config.DIRECTORY;
            string buferizedConfigDir = Config.DIRECTORY;

            // Init file.
            Config.Active = null;
            _ = Config.Active;

            // Mark result.
            CONFIG_FILE_GENERATED = true;

            // Check existing.
            bool result = File.Exists(Config.DIRECTORY + Config.CONFIG_FILE_NAME);

            // Assert.
            Assert.IsTrue(result, "File creation failed. " + buferizedConfigDir);
        }

        /// <summary>
        /// Try to load valid config data file.
        /// </summary>
        public void Config_Load_ValidData()
        {
            // Try to load config from directory.
            bool result = Handler.TryToLoad<Config>(
                Config.DIRECTORY + Config.CONFIG_FILE_NAME, out Config _);

            Assert.IsTrue(result, "Loading failed.");
        }

        /// <summary>
        /// Trying to load corrupted config dile.
        /// </summary>
        public void Config_Load_CoruptedData()
        {
            string corruptedFileDirectory = Config.DIRECTORY + Config.CONFIG_FILE_NAME + ".invalid";

            // Copy valid file.
            File.Copy(Config.DIRECTORY + Config.CONFIG_FILE_NAME, corruptedFileDirectory);

            #region Damage file
            string[] lines = File.ReadAllLines(corruptedFileDirectory);
            // Add invalid entrance.
            for(int i = 0; i < lines.Length; i += 4)
            {
                lines[i] = lines[i] + "misclick";
            }

            // Add invalid tags.
            for (int i = 1; i < lines.Length; i += 4)
            {
                lines[i] = lines[i] + "<invalidSheme>";
            }

            // Swithc digits valut to string
            for (int i = 1; i < lines.Length; i ++)
            {
                // Get end of tag.
                int valueIndex = lines[i].IndexOf('>');
                // Skip if closed.
                if (valueIndex + 1 >= lines[i].Length)
                {
                    continue;
                }

                // Check if value is digit.
                bool isDigit = Int32.TryParse(lines[i][valueIndex + 1].ToString(), out int _);

                // Change digit to string.
                if (isDigit)
                {
                    lines[i] = lines[i].Remove(valueIndex + 1, 1);
                    lines[i] = lines[i].Insert(valueIndex + 1, "nonDigit");
                }
            }

            // Write corrupted lines.
            File.WriteAllLines(corruptedFileDirectory, lines);
            #endregion

            // Try to load config from directory.
            bool result = Handler.TryToLoad<Config>(
            corruptedFileDirectory, out Config _);

            Assert.IsTrue(!result, "Corrupted file cause error.");
        }


        /// <summary>
        /// Stress test in working with huge data.
        /// </summary>
        [TestMethod]
        public void UsersPoolStressTest()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                UsersPool_New();
                UsersPoo_Load();
            }
        }

        /// <summary>
        /// Create huge pool of users.
        /// Use for stress tests.
        /// </summary>
        public void UsersPool_New()
        {
            bool poolFailed = false;
            int poolUsersCount = 10000;

            // Fail callback
            void FailHandler(User obj, string error)
            {
                poolFailed = true;
                AuthorityController.API.Users.UserProfileNotStored -= FailHandler;

                Assert.IsTrue(false, "Data storing failed.");
            }
            
            AuthorityController.API.Users.UserProfileNotStored += FailHandler;

            // Create all requested users.
            for (int i = 0; i < poolUsersCount; i++)
            {
                // Create user.
                User user = new User()
                {
                    login = "user" + i
                };
                // Get GUID
                user.id = AuthorityController.API.Users.GenerateID(user);

                // Save profile.
                AuthorityController.API.Users.SetProfileAsync(user, Helpers.FileSystem.TestSubfolder + "\\USERS\\");
            }

            // Create users directory if notexist.
            if (!Directory.Exists(Helpers.FileSystem.TestSubfolder + "\\USERS\\"))
            {
                Directory.CreateDirectory(Helpers.FileSystem.TestSubfolder + "\\USERS\\");
            }

            // Wait until operation compleeting.
            while (!poolFailed)
            {
                int profilesCount = Directory.GetFiles(Helpers.FileSystem.TestSubfolder + "\\USERS\\").Length;
                if (profilesCount == poolUsersCount)
                {
                    break;
                }
                Thread.Sleep(50);
            }

            Assert.IsTrue(!poolFailed);
        }

        /// <summary>
        /// Try to load a huge pool of users. Some users can be corrupted.
        /// Need to finish operation without freezing of other threads and without crush due corupted data.
        /// </summary>
        public void UsersPoo_Load()
        {
            // Init
            string loadDirectory = Helpers.FileSystem.TestSubfolder + "\\USERS\\";
            bool loaded = false;

            #region Test finish handler.
            // Wait until loading finish.
            void FinishHandler(string dir, int succeed, int failed)
            {
                // If currect directory
                if (dir.Equals(loadDirectory))
                {
                    // Unsubscribe
                    AuthorityController.API.Users.DirectoryLoadingFinished -= FinishHandler;

                    // Infrom waitnig loop about finish.
                    loaded = true;

                    // Check is passed all?
                    int commonCountOfFiles = Directory.GetFiles(loadDirectory).Length;
                    Assert.IsTrue(commonCountOfFiles == succeed + failed, "No all files was processed");

                    // Check corrupted.
                    Assert.IsTrue(failed == 0, "Some files corrupted: " + failed);
                }
            }

            AuthorityController.API.Users.DirectoryLoadingFinished += FinishHandler;
            #endregion

            AuthorityController.API.Users.LoadProfilesAsync(loadDirectory);

            // Wait until load.
            while (!loaded)
            {
                Thread.Sleep(5);
            }
        }


        /// <summary>
        /// Validate full stack of fetaures related to user profile.
        /// </summary>
        [TestMethod]
        public void UserProfileValidation()
        {
            lock (Helpers.Locks.CONFIG_LOCK)
            {
                User testUser = User_New();
                User_Update(testUser);
                User_Remove(testUser);
            }
        }

        /// <summary>
        /// Creating new user profile and saving to storage.
        /// </summary>
        public User User_New()
        {
            // Create user.
            User testUser = new User()
            {
                login = "userLogin"
            };
            // Get GUID
            testUser.id = AuthorityController.API.Users.GenerateID(testUser);

            // Save profile.
            AuthorityController.API.Users.SetProfileAsync(testUser, Helpers.FileSystem.TestSubfolder + "\\USERS\\TEMP\\");
            bool userTestPaused = true;

            #region Wait for result
            // Failed
            AuthorityController.API.Users.UserProfileNotStored += (User u, string error) =>
            {
                if (u.Equals(testUser))
                {
                    userTestPaused = false;
                    Assert.IsTrue(false, error);
                }
            };

            // Stored
            AuthorityController.API.Users.UserProfileStored += (User u) =>
            {
                if (u.Equals(testUser))
                {
                    userTestPaused = false;
                    Assert.IsTrue(true);
                }
            };
            #endregion

            // Wait untol complete.
            while(userTestPaused)
            {
                Thread.Sleep(50);
            }

            return testUser;
        }

        /// <summary>
        /// Update already existed profile.
        /// </summary>
        public void User_Update(User testUser)
        {
            testUser.firstName = "Updated";
            testUser.secondName = "Updated";

            // Save profile.
            AuthorityController.API.Users.SetProfileAsync(testUser, Helpers.FileSystem.TestSubfolder + "\\USERS\\TEMP\\");

            bool userTestPaused = true;

            #region Wait for result
            // Failed
            AuthorityController.API.Users.UserProfileNotStored += (User u, string error) =>
            {
                if (u.Equals(testUser))
                {
                    userTestPaused = false;
                    Assert.IsTrue(false, error);
                }
            };

            // Stored
            AuthorityController.API.Users.UserProfileStored += (User u) =>
            {
                if (u.Equals(testUser))
                {
                    userTestPaused = false;
                    Assert.IsTrue(true);
                }
            };
            #endregion

            // Wait untol complete.
            while (userTestPaused)
            {
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Remove profile from storage.
        /// </summary>
        public void User_Remove(User testUser)
        {
            Assert.IsTrue(AuthorityController.API.Users.RemoveProfile(testUser, Helpers.FileSystem.TestSubfolder + "\\USERS\\TEMP\\"));
        }


        /// <summary>
        /// Start complex list of tests thet validate password salt feature.
        /// </summary>
        [TestMethod]
        public void SaltGeneration()
        {
            // Generate config if not generated yet.
            _ = Config.Active;

            lock (Helpers.Locks.CONFIG_LOCK)
            {
                Salt_Init();
                Salt_Loading();
                Salt_Validation_ValidData();
                Salt_Validation_InvalidData();
            }
        }

        /// <summary>
        /// Creating a new salt data.
        /// </summary>
        public void Salt_Init()
        {
            try
            {
                // Init new salt file generation.
                _ = AuthorityController.Data.Config.Active.Salt;
            }
            catch (Exception ex)
            {
                // Fail.
                Assert.IsTrue(false, ex.Message);
                return;
            }

            bool result = File.Exists(
                Config.DIRECTORY +
                AuthorityController.Data.Config.Active.PasswordSaltFileName);

            Assert.IsTrue(result, "Salt file not found");
        }

        /// <summary>
        /// Validate loading of already created salt
        /// </summary>
        public void Salt_Loading()
        {
            // Create new configs to drop loaded salt.
            _ = new Config();

            try
            {
                // Call existed salt.
                _ = AuthorityController.Data.Config.Active.Salt;

                Assert.IsTrue(true);
            }
            catch(Exception ex)
            {
                Assert.IsTrue(false, ex.Message);
            }
        }

        /// <summary>
        /// Validate salt with correct stamp.
        /// </summary>
        public void Salt_Validation_ValidData()
        {
            // Validate
            bool result = AuthorityController.Data.Config.Active.Salt.Validate();

            // Assert.
            Assert.IsTrue(result, "Stamp not pass validation");
        }

        /// <summary>
        /// Validate salt with incorrect salt.
        /// </summary>
        public void Salt_Validation_InvalidData()
        {
            // Reinit stamp with the same lenght but with default values.
            AuthorityController.Data.Config.Active.Salt.validationStamp = new byte[AuthorityController.Data.Config.Active.Salt.validationStamp.Length];

            // Validate
            bool result = AuthorityController.Data.Config.Active.Salt.Validate();

            // Assert.
            Assert.IsTrue(!result, "Validator pass invalid stamp.");
        }
    }
}
