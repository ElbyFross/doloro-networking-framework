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
using System.Collections.Generic;
using PipesProvider.Security.Encryption;

namespace UniformServer
{
    /// <summary>
    /// An API class that helps with an configuration.
    /// </summary>
    public static class ServerAppConfigurator
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
        /// Object that allow to detect processes conflict.
        /// </summary>
        public static Mutex mutexObj = new Mutex();

        /// <summary>
        /// Argument that will hide console window.
        /// </summary>
        private const int SW_HIDE = 0;

        /// <summary>
        /// Agrument that will show console window.
        /// </summary>
        private const int SW_SHOW = 5;

        /// <summary>
        /// A time that recommended for threads as a value for sleep.
        /// </summary>
        public static int preferedThreadsSleepTime = 15;

        /// <summary>
        /// Marker that defines an app termination status.
        /// </summary>
        private static bool appTerminated = false;

        /// <summary>
        /// A list that contains a token added on managment.
        /// </summary>
        private static readonly List<CancellationTokenSource> managedTokens = 
            new List<CancellationTokenSource>();
        
        /// <summary>
        /// Method that will configurate application and server relative to the uniform arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// ### Allowed commands:
        /// + hide - hiding a console window after call.
        /// </remarks>
        public static void ArgsReactor(string[] args)
        {
            // Get a pointer to this console.
            IntPtr hwnd = NativeMethods.GetConsoleWindow();

            // Change window state.
            NativeMethods.ShowWindow(hwnd, SW_SHOW);

            // Check every argument.
            foreach (string command in args)
            {
                // Hide application from tray.
                if (command == "hide")
                {
                    NativeMethods.ShowWindow(hwnd, SW_HIDE);
                    continue;
                }

                // Encripton configurator customization.
                if(command.StartsWith("aeo="))
                {
                    // Getting an operator's code.
                    string aeoCode = command.Substring(4);

                    // Trying to apply aeo
                    try
                    {
                        // looking for an operator with the dame code.
                        var aeo = EnctyptionOperatorsHandler.InstantiateAsymmetricOperator("aeoCode");

                        // Applying if found.
                        EnctyptionOperatorsHandler.AsymmetricEO = aeo;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "ENCRYPTION ERROR: Operator not found." +
                            " Requersted code \"" + aeoCode + "\"\nDetails: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Adding a cancelation token sorse to the managment system.
        /// Those tokens will be affected by app termination.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public static void AddTerinationToken(CancellationTokenSource cancellationToken)
        {
            managedTokens.Add(cancellationToken);
        }

        /// <summary>
        /// Changes app status on terminated.
        /// Invokes temination tokens.
        /// </summary>
        public static void TerminateApp()
        {
            // Setting the marker to true.
            appTerminated = true;

            // Terminating tasks.
            foreach(CancellationTokenSource token in managedTokens)
            {
                try
                {
                    // Terminating a task.
                    token.Cancel();

                    // Releasing a memory.
                    token.Dispose();
                }
                catch { } // Obsolte token. Not important.
            }
        }

        /// <summary>
        /// Checks does the process is unique and system has no other app copies started at the moment.
        /// </summary>
        /// <returns>Result of the check.</returns>
        public static bool IsProccessisUnique()
        {
            // Get GUID of this assebly.
            string guid = Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly()).ToString();

            // Create Mutex for this app instance.
            mutexObj = new Mutex(true, guid, out bool newApp);

            // Return a result.
            return newApp;
        }
    }
}
