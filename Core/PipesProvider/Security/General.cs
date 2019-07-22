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
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Management;

namespace PipesProvider.Security
{
    /// <summary>
    /// Class that contain methods for working with sequrity systems.
    /// </summary>
    public static class General
    {
        #region Named pipes
        /// <summary>
        /// Configurate pipe squrity relative to requested level.
        ///
        /// You can request more then one level via using format:
        /// SecurityLevel | SecurityLevel | ...
        /// 
        /// Internal level will applyed by default to allow system and application control created pipes.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static PipeSecurity GetRulesForLevels(SecurityLevel level)
        {
            // Get core base of rules that euqual Internal level.
            PipeSecurity rules = DefaultInternalPipeScurity;

            string rulesLog = "";

            // Add Anonymous rule
            if (level.HasFlag(SecurityLevel.Anonymous))
            {
                // Add to log.
                rulesLog += (rulesLog.Length > 0 ? " | " : "") + "WorldSid";

                // Add owner rights to control the pipe.
                rules.AddAccessRule(
                    new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            // Add Authenticated rule
            if (level.HasFlag(SecurityLevel.RemoteLogon))
            {
                // Add to log.
                rulesLog += (rulesLog.Length > 0 ? " | " : "") + "RemoteLogonIdSid";

                // Add owner rights to control the pipe.
                rules.AddAccessRule(
                    new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.RemoteLogonIdSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            // Add Local rule
            if (level.HasFlag(SecurityLevel.Local))
            {
                // Add to log.
                rulesLog += (rulesLog.Length > 0 ? " | " : "") + "LocalSystemSid";


                // Add owner rights to control the pipe.
                rules.AddAccessRule(
                    new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            // Add Administrator rule
            if (level.HasFlag(SecurityLevel.Administrator))
            {
                // Add to log.
                rulesLog += (rulesLog.Length > 0 ? " | " : "") + "BuiltinAdministratorsSid";


                // Add owner rights to control the pipe.
                rules.AddAccessRule(
                    new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            // Show logs.
            Console.WriteLine("APPLIED RULES: system | self | " + rulesLog);

            return rules;
        }

        /// <summary>
        /// Return pipe security situable for internal use.
        /// </summary>
        public static PipeSecurity DefaultInternalPipeScurity
        {
            get
            {
                // Set common sequrity.
                PipeSecurity pipeSecurity = new PipeSecurity();
                
                // Add system rights to control the pipe.
                pipeSecurity.AddAccessRule(
                    new PipeAccessRule("SYSTEM",
                    PipeAccessRights.FullControl, AccessControlType.Allow));

                // Add owner rights to control the pipe.
                pipeSecurity.AddAccessRule(
                 new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                 PipeAccessRights.FullControl, AccessControlType.Allow));

                return pipeSecurity;
            }
        }
        #endregion

        #region Windows
        /// <summary>
        /// Change local security authority of machine to allow requested security level.
        /// Require admin rights.
        /// </summary>
        /// <param name="level"></param>
        public static void SetLocalSecurityAuthority(SecurityLevel level)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                #region Check rights
                // Check rights.
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                if(!isElevated)
                {
                    Console.WriteLine(
                        "SECURITY ERROR: LSA update require admin rights."+
                        "Close application and start it as Admin.");
                    return;
                }
                #endregion

                // If require anonymus connection.
                if (level.HasFlag(SecurityLevel.Anonymous))
                {
                    SecurityIdentifier guestDomainSID = new SecurityIdentifier(WellKnownSidType.BuiltinGuestsSid, null);
                    SecurityIdentifier guestSID = null;

                    #region Activate guest user
                    // Start command line.
                    System.Diagnostics.Process cmd = new System.Diagnostics.Process();
                    cmd.StartInfo.FileName = "cmd.exe";
                    cmd.StartInfo.RedirectStandardInput = true;
                    cmd.StartInfo.RedirectStandardOutput = true;
                    cmd.StartInfo.CreateNoWindow = true;
                    cmd.StartInfo.UseShellExecute = false;
                    cmd.Start();

                    // Create system query.
                    SelectQuery query = new SelectQuery("Win32_UserAccount");
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                    foreach (ManagementObject envVar in searcher.Get())
                    {
                        // Get name of account.
                        var account = new NTAccount(envVar["Name"].ToString());
                        // Get SID of account.
                        var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

                        // Check is account is Guest.
                        if (sid.IsWellKnown(WellKnownSidType.AccountGuestSid))
                        {
                            guestSID = sid;

                            // Send order to activate.
                            cmd.StandardInput.WriteLine("net user {0} /active:yes", envVar["Name"].ToString());

                            // Log result.
                            Console.WriteLine("LSA: \"{0}\" user activated to allow anonymous access to this machine.", 
                                envVar["Name"]);
                            break;
                        }
                    }

                    // Send command.
                    cmd.StandardInput.Flush();
                    cmd.StandardInput.Close();
                    //cmd.WaitForExit();
                    //Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                    #endregion

                    #region Remove Guests from "deny access to this computer from the network".

                    Console.WriteLine("LSA: Network logon for Guests allowed.");
                    LSA.LsaSecurityWrapper.AddAccountRights(guestDomainSID, "SeNetworkLogonRight");

                    Console.WriteLine("LSA: Deny network logon right for Guests domain removed.");
                    LSA.LsaSecurityWrapper.RemoveAccountRights(guestDomainSID, "SeDenyNetworkLogonRight");

                    if (guestSID != null)
                    {
                        Console.WriteLine("LSA: Deny network logon right for Guest user removed.");
                        LSA.LsaSecurityWrapper.RemoveAccountRights(guestSID, "SeDenyNetworkLogonRight");
                    }
                    #endregion
                }
            }
        }
                
        /// <summary>
        /// Trying to get access token for remote user.
        /// In case if requested anonymous connection then return anonymous token without permission check.
        /// </summary>
        /// <param name="config">Fields required for remote logon with impersonation.</param>
        /// <returns></returns>
        public static bool TryLogon(LogonConfig config, out SafeAccessTokenHandle token)
        {
            #region Anonymous token
            // Return anonimus token.
            if (config.IsAnonymous)
            {
                token = WindowsIdentity.GetAnonymous().AccessToken;
                return true;
            }
            #endregion

            #region Remote user logon
            // Win NT version.
            const int LOGON32_PROVIDER_WINNT50 = 3;
            // This parameter causes LogonUser to create a primary token.
            const int LOGON_TYPE_NEW_CREDENTIALS = 9;

            // Call LogonUser to obtain a handle to an access token.
            bool returnValue = NativeMethods.LogonUser(config.userName, config.domain, config.password,
                LOGON_TYPE_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50,
                out token);

            // Validate result.
            if (false == returnValue)
            {
                // Log fail resone.
                int ret = Marshal.GetLastWin32Error();
                Console.WriteLine("LogonUser failed with error code : {0}", ret);
                //throw new System.ComponentModel.Win32Exception(ret);

                return false;
            }
            return true;
            #endregion
        }
        #endregion
    }
}
