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
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PipesProvider.Server;
using PipesProvider.Server.TransmissionControllers;
using PipesProvider.Security;
using PipesProvider.Security.Encryption.Operators;

namespace UniformServer
{
    /// <summary>
    /// Part of class that provides APi for handling a server security configuration.
    /// </summary>
    public partial class BaseServer
    {
        /// <summary>
        /// Scurity level that will applied to pipe.
        /// </summary>
        public SecurityLevel securityLevel = SecurityLevel.Anonymous;
    }
}
