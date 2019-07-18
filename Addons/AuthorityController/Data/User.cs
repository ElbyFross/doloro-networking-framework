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
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace AuthorityController.Data
{
    /// <summary>
    /// Object that contain relevant data about user.
    /// </summary>
    [System.Serializable]
    public class User
    {
        #region Serialized fields
        /// <summary>
        /// Unique id of this user to allow services access.
        /// </summary>
        public uint id;

        /// <summary>
        /// Login of this user to access the system.
        /// </summary>
        public string login;

        /// <summary>
        /// ON CLIENT: Open password. Situable only if user provides profile as new.
        /// ON SERVER: Hashed and salted password that confirm user rights to use this account.
        /// </summary>
        public byte[] password;

        /// <summary>
        /// Name of the user that will displayed in profile.
        /// </summary>
        public string firstName;

        /// <summary>
        /// Secondary name that will be displayed in profile.
        /// </summary>
        public string secondName;

        /// <summary>
        /// Array of rigts' codes provided to this user.
        /// </summary>
        public string[] rights = new string[0];

        /// <summary>
        /// List of bans that would received by user.
        /// </summary>
        public List<BanInformation> bans = new List<BanInformation>();

        /// <summary>
        /// List of culture codes that prefered by this user.
        /// In order of priority.
        /// 
        /// Define what a UI language will selected after user login.
        /// Useful in multicultural environment like universities.
        /// </summary>
        public List<string> culturePreferences = new List<string>();
        #endregion

        #region Seesion-time fields
        /// <summary>
        /// List that cont tokens provided to this user.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public List<string> tokens = new List<string>();
        #endregion


        #region API
        /// <summary>
        /// Convert user data to string format to allow sharing via query.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string ToQueryValue(User user)
        {
            // Convert table to XML file.
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(User));
                using (StringWriter stream = new StringWriter())
                {
                    serializer.Serialize(stream, user);
                    return stream.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ROUTING TABLE ERROR: Not serialized. Reason:\n{0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Trying to convert user in string format of object instance.
        /// </summary>
        /// <param name="queryValue">String format of user recived from ToQueryValue method.</param>
        /// <param name="user">Instiniated user with shared data.</param>
        /// <returns>Does convertation passed success.</returns>
        public static bool TyToParseQueryValue(string queryValue, out User user)
        {
            // Convert table to XML file.
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(User));
                using (StringReader stream = new StringReader(queryValue))
                {
                    user = (User)serializer.Deserialize(stream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("USER DATA CORUPTED. Reason:\n{0}", ex.Message);
                user = null;
                return false; ;
            }
        }


        /// <summary>
        /// Compare recived open password with stored to user.
        /// </summary>
        /// <param name="recivedPassword"></param>
        /// <returns></returns>
        public bool IsOpenPasswordCorrect(string recivedPassword)
        {
            // Get hashed password from recived.
            byte[] recivedHashedPassword = API.Users.GetHashedPassword(recivedPassword, Data.Config.Active.Salt);

            // Compare.
            return IsHashedPasswordCorrect(recivedHashedPassword);
        }

        /// <summary>
        /// Check does the recived password is the same as stored to user.
        /// </summary>
        /// <param name="recivedHashedPassword"></param>
        /// <returns></returns>
        public bool IsHashedPasswordCorrect(byte[] recivedHashedPassword)
        {
            // Compare length to avoid long time comparing.
            if (password.Length != recivedHashedPassword.Length)
            {
                return false;
            }

            // Compare every byte.
            for (int i = 0; i < password.Length; i++)
            {
                if (password[i] != recivedHashedPassword[i])
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
