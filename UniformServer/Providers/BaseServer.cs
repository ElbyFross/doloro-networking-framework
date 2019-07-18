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
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;

namespace UniformServer
{
    /// <summary>
    /// Class that provide base server features and envirounment static API.
    /// </summary>
    public abstract partial class BaseServer
    {
        #region Static fields and properties
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
        /// If true then will stop main loop.
        /// </summary>
        public static bool appTerminated;

        /// <summary>
        /// Count of threads that will started on server.
        /// Changing during fly permited and safely.
        /// </summary>
        public static int ThreadsCount
        {
            get { return threadsCount; }
            set
            {
                // Compute value not high then max count of threads and not less then one.
                int threadsCountBufer = Math.Min(value, Environment.ProcessorCount);
                threadsCountBufer = Math.Max(1, threadsCountBufer);

                // Avoid if not require changes.
                if (threadsCount == threadsCountBufer)
                {
                    Console.WriteLine("THREAD COUNT CHANGING: Operation avoided. The same count actualy requested.");
                    return;
                }

                // Request tread thermination for every thread will higher index then allowed.
                for (int i = threadsCount - 1; i > threadsCountBufer - 1; i--)
                {
                    // Inform subscribers.
                    if (ThreadTerminateRequest != null)
                    {
                        Console.WriteLine("THREAD #{0} TERMINATION REQUEST", i);
                        ThreadTerminateRequest(i);
                    }
                }

                // Request tread thermination for every thread will higher index then allowed.
                for (int i = threadsCount - 1; i < threadsCountBufer - 1; i++)
                {
                    // Inform subscribers.
                    if (ThreadStartRequest != null)
                    {
                        Console.WriteLine("THREAD #{0} START REQUEST", i);
                        ThreadStartRequest(i);
                    }
                }

                // Apply computed count.
                threadsCount = threadsCountBufer;
                Console.WriteLine("ACTUAL COUNT OF THREADS: {0}/{1}", threadsCount, Environment.ProcessorCount);
            }
        }


        /// <summary>
        /// How many milisseconds will sleep thread after tick.
        /// </summary>
        protected static int threadSleepTime = 15;

        /// <summary>
        /// Count of threads.
        /// </summary>
        protected static int threadsCount = 1;

        /// <summary>
        /// Threads that contai servers.
        /// </summary>
        protected static BaseServer[] longTermServerThreads;

        /// <summary>
        /// Argument that will hide console window.
        /// </summary>
        protected const int SW_HIDE = 0;

        /// <summary>
        /// Agrument that will show console window.
        /// </summary>
        protected const int SW_SHOW = 5;

        /// <summary>
        /// Object that allow to detect processes conflict.
        /// </summary>
        protected static Mutex mutexObj = new Mutex();
        #endregion

        #region Fields
        /// <summary>
        /// Reference to thread that host this server.
        /// </summary>
        public Thread thread;

        /// <summary>
        /// Name that will be applied to the pipe.
        /// </summary>
        public string pipeName;

        /// <summary>
        /// Squrity level that will applied to pipe.
        /// </summary>
        public PipesProvider.Security.SecurityLevel securityLevel = PipesProvider.Security.SecurityLevel.Anonymous;
        #endregion


        #region Application configuration
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
            string[] dllFiles = Directory.GetFiles(path, "*.dll");

            // Loading assemblies.
            if (dllFiles.Length > 0)
            {
                Console.WriteLine("ASSEMBLIES DETECTED:");
            }
            foreach (string _path in dllFiles)
            {
                Assembly.LoadFrom(_path);
                Console.WriteLine(_path.Substring(_path.LastIndexOf("\\") + 1));
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Method that will configurate application and server relative to the uniform arguments.
        /// </summary>
        /// <param name="args"></param>
        protected static void ArgsReactor(string[] args)
        {
            // Get a pointer to this console.
            IntPtr hwnd = GetConsoleWindow();

            // Change window state.
            ShowWindow(hwnd, SW_SHOW);

            // Check every argument.
            foreach (string s in args)
            {
                // Hide application from tray.
                if (s == "hide")
                {
                    ShowWindow(hwnd, SW_HIDE);
                    continue;
                }

                // Change count of available treads.
                if(s.StartsWith("threads"))
                {
                    try
                    {
                        // Parse requested count of threads.
                        int requestedTreadsCounts = Int32.Parse(s.Substring(7));
                        // Apply request.
                        ThreadsCount = requestedTreadsCounts;
                    }
                    catch
                    {
                        Console.WriteLine("ARGUMENT ERROR: {0} cannot pe converted to int.", s.Substring(7));
                    }
                    continue;
                }
            }
        }
        #endregion
        
        #region Multithreading
        /// <summary>
        /// Method that starting server thread.
        /// </summary>
        /// <param name="threadName"></param>
        /// <param name="sharebleParam"></param>
        /// <returns></returns>
        protected Thread StartServerThread(string threadName, object sharebleParam, ParameterizedThreadStart serverLoop)
        {
            // Initialize queries monitor thread.
            thread = new Thread(serverLoop)
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