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
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

using PipesProvider.Networking.Routing;
using PipesProvider.Client;

namespace UniformClient
{
    /// <summary>
    /// Part off class that provide controll under the process.
    /// </summary>
    public abstract partial class BaseClient
    {
        /// <summary>
        /// Imported method that allo to controll console window state.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        protected static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Inported method that allow acces to console window.
        /// </summary>
        /// <returns></returns>
        [DllImport("Kernel32")]
        protected static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Argument that will hide console window.
        /// </summary>
        protected const int SW_HIDE = 0;

        /// <summary>
        /// Agrument that will show console window.
        /// </summary>
        protected const int SW_SHOW = 5;
    }
}
