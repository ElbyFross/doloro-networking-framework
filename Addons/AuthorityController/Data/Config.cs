﻿//Copyright 2019 Volodymyr Podshyvalov
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
using System.Xml.Serialization;
using System.Xml;

namespace AuthorityController.Data
{
    /// <summary>
    /// Object that contain data for setup of authority controller.
    /// </summary>
    [System.Serializable]
    public class Config
    {
        #region Constants
        /// <summary>
        /// Directory that will contain serialized instance of this class.
        /// </summary>
        public static string DIRECTORY = "\\resources\\ac\\";

        /// <summary>
        /// Name of the file that will be loaded as config.
        /// </summary>
        public static string CONFIG_FILE_NAME = ".config";        
        #endregion
               
        #region Serialized fields
        /// <summary>
        /// Directory to folder that will contain users data.
        /// </summary>
        public string UsersStorageDirectory = "\\resorces\\users\\";

        #region User logins
        /// <summary>
        /// How many character will be required in password.
        /// </summary>
        public int LoginMinSize = 5;

        /// <summary>
        /// How many character will be allowed in password.
        /// </summary>
        public int LoginMaxSize = 16;
        #endregion

        #region User names

        /// <summary>
        /// Define a format of allowed name.
        /// By default provide possibility to make a names like:
        /// XXXXX
        /// Xxxx-Xxxx
        /// Xx'Xxx
        /// etc.
        /// 
        /// For creating your own please use a Regex sinaxis.
        /// </summary>
        public string UserNameRegexPattern = @"^([A-Z][a-z]+)+([-' ][A-Z][a-z]+)?$";
        #endregion

        #region User rights
        /// <summary>
        /// List of user rights that will profided to every new user.
        /// Any extra rights will require aditional query from authorised user.
        /// </summary>
        public string[] UserDefaultRights = new string[]
        {
            "base",
            "commenting",
            "publishing",
            "personManaging",
            "passwordManaging",
        };
        #endregion

        #region User passwords
        /// <summary>
        /// Name of the file that will contain salt.
        /// </summary>
        public string PasswordSaltFileName = ".salt";

        /// <summary>
        /// How many bytes will contain salt.
        /// </summary>
        public int PasswordSaltSize = 32;

        /// <summary>
        /// How many character will be required in password.
        /// </summary>
        public int PasswordMinAllowedLength = 8;

        /// <summary>
        /// How many character will be allowed in password.
        /// </summary>
        public int PasswordMaxAllowedLength = 32;

        /// <summary>
        /// If true then will requre at leas one symbol like !@$% etc.
        /// Application will provide valid mask without your involving.
        /// </summary>
        public bool PasswordRequireSpecialSymbol = false;

        /// <summary>
        /// If true then will require at least one symbol in high register.
        /// Application will provide valid mask without your involving.
        /// </summary>
        public bool PasswordRequireUpperSymbol = true;
        
        /// <summary>
        /// If true then will require at least one digit.
        /// Application will provide valid mask without your involving.
        /// </summary>
        public bool PasswordRequireDigitSymbol = true;
        #endregion


        /// <summary>
        /// How many minutes token is valid.
        /// </summary>
        public int TokenValidTimeMinutes = 1440;

        /// rank=x where x is
        /// 0 - guest
        /// 1 - user 
        /// 2 - privileged user
        /// 4 - moderator
        /// 8 - admin
        /// 16 - superadmin
        #region Queries rights
        /// <summary>
        /// Rights code required for requester to proceed this action.
        /// 
        /// bannhammer - allow user set bans to others.
        /// >rank=2 - wil requre at least moderators level. 
        /// </summary>
        public string[] QUERY_UserBan_RIGHTS = new string[] { "banhammer", ">rank=2" };

        /// <summary>
        /// Rights code required for requester to proceed this action.
        /// 
        /// passwordManaging - user can change them passwords.
        /// </summary>
        public string[] QUERY_UserNewPassword_RIGHTS = new string[] { "passwordManaging" };

        /// <summary>
        /// What a rights will required to user that try to change the password of other user.
        /// Also system auto add instruction where user need to have hieghest rank the target.
        /// 
        /// ">rank=2 - ata least moderator level.
        /// </summary>
        public string[] QUERY_UserPasswordModeration_RIGHTS = new string[] { ">rank=2" };


        /// <summary>
        /// Rights code required for requester to proceed this action.
        /// >rank=4 - will requre at least admin level.
        /// </summary>
        public string[] QUERY_SetTokenRights_RIGHTS = new string[] {">rank=5" };
        #endregion
        #endregion




        #region Single tone
        /// <summary>
        /// Reference to current configs.
        /// Auto load one from resources if exist by DIRECTORY.
        /// </summary>
        [XmlIgnore]
        public static Config Active
        {
            get
            {
                if (active == null)
                {
                    // Try to load config from directory.
                    if (!TryToLoad<Config>(DIRECTORY + CONFIG_FILE_NAME, out active))
                    {
                        // Create new one if failed.
                        active = new Config();

                        // Save to resources.
                        SaveAs<Config>(active, DIRECTORY, CONFIG_FILE_NAME);
                    }
                }
                return active;
            }
            set { active = null; }
        }
        [XmlIgnore]
        private static Config active;
        #endregion

        #region Salt
        /// <summary>
        /// Salt loaded from file.
        /// </summary>
        [XmlIgnore]
        public SaltContainer Salt
        {
            get
            {
                // If not found
                if (salt == null)
                {
                    // Try to load from resources.
                    if (!TryToLoad<SaltContainer>(DIRECTORY + PasswordSaltFileName, out salt))
                    {
                        // Generate new salt.
                        salt = new SaltContainer(PasswordSaltSize);

                        try
                        {
                            // Save to resources.
                            SaveAs<SaltContainer>(salt, DIRECTORY, PasswordSaltFileName);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }

                    // Validate salt.
                    if(!salt.Validate())
                    {
                        // Inform that salt or hash algorithm is failed.
                        throw new InvalidDataException();
                    }
                }
                return salt;
            }
        }
        [XmlIgnore]
        private SaltContainer salt;
        #endregion

        #region Constructors
        public Config()
        {
            // Set as active.
            active = this;
        }
        #endregion

        #region API
        /// <summary>
        /// Saving config file to directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="fileName"></param>
        public static void SaveAs<T>(object obj, string directory, string fileName)
        {
            // Check directory exist.
            if (!Directory.Exists(directory))
            {
                // Create new if not exist.
                Directory.CreateDirectory(directory);
            }

            // Convert table to XML file.
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, obj);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(directory + fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Auth control error (ACC 10): Not serialized. Reason:\n{0}", ex.Message);
                throw ex;
            }
        }
        
        /// <summary>
        /// Trying to deserialize object from XML file.
        /// </summary>
        /// <typeparam name="T">Required type</typeparam>
        /// <param name="path">Full path to file.</param>
        /// <param name="result">Deserizlised object.</param>
        /// <returns></returns>
        public static bool TryToLoad<T>(string path, out T result)
        { 
            // Check file exist.
            if (!File.Exists(path))
            {
                result = default;
                return false;
            }

            // Init encoder.
            XmlSerializer xmlSer = new XmlSerializer(typeof(T));

            // Open stream to XML file.
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                try
                {
                    // Try to deserialize object from file.
                    result = (T)xmlSer.Deserialize(fs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Auth control error (ACC 20): File reading failed. Reason:\n{0}\n", ex.Message);
                    result = default;
                    return false;
                }
            }

            return true;

        }
        #endregion
    }
}
