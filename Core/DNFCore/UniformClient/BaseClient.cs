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
    /// Class that provide base client features and envirounment static API.
    /// </summary>
    public abstract partial class BaseClient
    {
        #region Fields and properties
        /// <summary>
        /// How many milisseconds will sleep thread after tick.
        /// </summary>
        protected static int threadSleepTime = 150;

        /// <summary>
        /// If true then will stop main loop.
        /// </summary>
        public static bool AppTerminated
        {
            get
            {
                return _AppTerminated;
            }

            set
            {
                if (value == true)
                {
                    TerminationTokenSource.Cancel();
                }
                _AppTerminated = value;
            }
        }

        /// <summary>
        /// Pufer that contains state of app.
        /// </summary>
        private static bool _AppTerminated;

        /// <summary>
        /// Object that can be used to mange global treads.
        /// </summary>
        public static CancellationTokenSource TerminationTokenSource
        {
            get
            {
                if(_TerminationToken == null)
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
        /// Reference to thread that host this server.
        /// </summary>
        public Thread thread;
        #endregion

        #region Core | Application | Assembly
        /// <summary>
        /// Loading assemblies from requested path.
        /// </summary>
        /// <param name="path"></param>
        protected static void LoadAssemblies(string path)
        {
            // Validate directory.
            bool dirExist = Directory.Exists(path);
            if (!dirExist)
            {
                Console.WriteLine("Libs directory not found. Creating new one...\n{0}", path);
                Directory.CreateDirectory(path);
                Console.WriteLine("");
            }

            // Search files in directory.
            string[] dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);

            // Loading assemblies.
            if (dllFiles.Length > 0)
            {
                Console.WriteLine("ASSEMBLIES DETECTED:");
            }
            foreach (string _path in dllFiles)
            {
                try
                {
                    Assembly.LoadFrom(_path);
                    Console.WriteLine(_path.Substring(_path.LastIndexOf("\\") + 1));
                }
                catch(Exception ex)
                {
                    Console.WriteLine("DLL \"{0}\" LOADING FAILED: {1}", 
                        _path.Substring(_path.LastIndexOf("\\") + 1), 
                        ex.Message);
                }
            }

            if (dllFiles.Length > 0)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Method that will configurate application and server relative to the uniform arguments.
        /// </summary>
        /// <param name="args"></param>
        protected static void ArgsReactor(string[] args)
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

        /// <summary>
        /// Method that starting client thread.
        /// </summary>
        /// <param name="threadName"></param>
        /// <param name="sharebleParam"></param>
        /// <param name="clientLoop"></param>
        /// <returns></returns>
        protected virtual Thread StartClientThread(
            string threadName,
            object sharebleParam,
            ParameterizedThreadStart clientLoop)
        {
            // Abort started thread if exits.
            if(thread != null && thread.IsAlive)
            {
                Console.WriteLine("THREAD MANUAL ABORTED (BC_SCT_0): {0}", thread.Name);
                thread.Abort();
            }

            // Initialize queries monitor thread.
            thread = new Thread(clientLoop)
            {
                Name = threadName,
                Priority = ThreadPriority.BelowNormal
            };

            // Start thread
            thread.Start(sharebleParam);

            // Let it proceed first run.
            Thread.Sleep(threadSleepTime);

            return thread;
        }
        #endregion        
    }
}