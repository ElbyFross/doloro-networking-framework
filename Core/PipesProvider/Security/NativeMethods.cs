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
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security;
using PipesProvider.Security.LSA;

namespace PipesProvider.Security
{
    using LSA_HANDLE = IntPtr;

    /// <summary>
    /// Provide access to native metods used in security namespace.
    /// </summary>
    public static class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
        int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true),
             SuppressUnmanagedCodeSecurityAttribute]
        public static extern uint LsaOpenPolicy(
                LsaSecurityWrapper.LSA_UNICODE_STRING[] SystemName,
                ref LsaSecurityWrapper.LSA_OBJECT_ATTRIBUTES ObjectAttributes,
                int AccessMask,
                out IntPtr PolicyHandle
                );

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true),
         SuppressUnmanagedCodeSecurityAttribute]
        public static extern uint LsaAddAccountRights(
            LSA_HANDLE PolicyHandle,
            IntPtr pSID,
           LsaSecurityWrapper.LSA_UNICODE_STRING[] UserRights,
            int CountOfRights
            );

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true),
         SuppressUnmanagedCodeSecurityAttribute]
        public static extern uint LsaRemoveAccountRights(
            LSA_HANDLE PolicyHandle,
            IntPtr AccountSid,
            bool AllRights,
            LsaSecurityWrapper.LSA_UNICODE_STRING[] UserRights,
            int CountOfRights
            );

        [DllImport("advapi32")]
        public static extern int LsaClose(IntPtr PolicyHandle);
    }
}
