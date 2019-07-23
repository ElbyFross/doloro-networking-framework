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
using System.Runtime.InteropServices;

namespace PipesProvider
{
    /// <summary>
    /// Class that provide access to native methods.
    /// </summary>
    public static class NativeMethods
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);

        /// <summary>
        /// Method to test if Windows considers that a named pipe of a certain name exists or not.
        /// </summary>
        public static bool DoesNamedPipeExist(string serverName, string pipeName)
        {
            try
            {
                bool existing = WaitNamedPipe(@"\\" + serverName + @"\pipe\" + pipeName, 0);
                return existing;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
