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
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;
using UniformDataOperator.AssembliesManagement.Modifiers;

namespace UniformClient
{
    /// <summary>
    /// An API class that helps with an configuration.
    /// </summary>
    public static class ClientAppConfigurator
    {
        /// <summary>
        /// If true then will stop main loop.
        /// </summary>
        public static bool AppTerminated
        {
            get => appTerminated;
        }

        /// <summary>
        /// How many milisseconds will sleep thread after tick.
        /// </summary>
        public static int PreferedThreadsSleepTime
        {
            get => preferedThreadsSleepTime;
            set => preferedThreadsSleepTime = value;
        }

        /// <summary>
        /// Object that can be used to mange global treads.
        /// </summary>
        public static CancellationTokenSource TerminationTokenSource
        {
            get
            {
                if (_TerminationToken == null)
                {
                    _TerminationToken = new CancellationTokenSource();
                }
                return _TerminationToken;
            }
            set
            {
                _TerminationToken = value;
            }
        }

        /// <summary>
        /// Buferr that contains token controller.
        /// </summary>
        private static CancellationTokenSource _TerminationToken;


        /// <summary>
        /// A time that recommended for threads as a value for sleep.
        /// </summary>
        private static int preferedThreadsSleepTime = 150;

        /// <summary>
        /// Marker that defines an app termination status.
        /// </summary>
        private static bool appTerminated = false;

        /// <summary>
        /// Argument that will hide console window.
        /// </summary>
        private const int SW_HIDE = 0;

        /// <summary>
        /// Agrument that will show console window.
        /// </summary>
        private const int SW_SHOW = 5;

        /// <summary>
        /// Changes app status on terminated.
        /// Invokes temination tokens.
        /// </summary>
        public static void TerminateApp()
        {
            TerminationTokenSource.Cancel();
            TerminationTokenSource.Dispose();

            appTerminated = true;
        }

        /// <summary>
        /// Method that will configurate application and server relative to the uniform arguments.
        /// </summary>
        /// <param name="args"></param>
        public static void ArgsReactor(string[] args)
        {
            // Get a pointer to this console.
            IntPtr hwnd = NativeMethods.GetConsoleWindow();

            // Change window state.
            NativeMethods.ShowWindow(hwnd, SW_SHOW);

            // Check every argument.
            foreach (string s in args)
            {
                // Hide application from tray.
                if (s == "hide")
                {
                    NativeMethods.ShowWindow(hwnd, SW_HIDE);
                    continue;
                }
            }
        }
    }
}
