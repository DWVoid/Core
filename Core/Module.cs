//     
//  Core:Module.cs
//  Created on 2019/05/02 19:55
// 
//  Copyright 2019-2019 Infinideastudio
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Akarin
{
    public interface IModule
    {
        void CoInitialize();
        void CoFinalize();
        void OnMemoryWarning();
    }

    public sealed class DeclareModuleAttribute : Attribute
    {
    }

    [DeclareAssemblyReflectiveScanner]
    [DeclareGlobalBusEventHandlerClass]
    public sealed class Modules : IAssemblyReflectiveScanner
    {
        private static readonly Dictionary<string, IModule> Loaded = new Dictionary<string, IModule>();

        private static string _basePath = AppContext.BaseDirectory;

        public void ProcessType(Type type)
        {
            if (type.IsDefined(typeof(DeclareModuleAttribute), false) && typeof(IModule).IsAssignableFrom(type))
                try
                {
                    var module = (IModule) Activator.CreateInstance(type);
                    module.CoInitialize();
                    lock (Loaded)
                    {
                        Loaded.Add(type.FullName ?? "", module);
                    }

                    LogPort.Debug($"Loaded Module : {type}");
                }
                catch (Exception e)
                {
                    LogPort.Debug($"Module {type} Load Failure : {e}");
                }
        }

        public static void SetBasePath(string path)
        {
            _basePath = path;
        }

        public static void Load(string moduleFile)
        {
            Assembly.Load(moduleFile);
        }

        [DeclareBusEventHandler]
        public static void UnloadAll(object sender, ApplicationControl.Shutdown type)
        {
            lock (Loaded)
            {
                foreach (var module in Loaded)
                    module.Value.CoFinalize();
                Loaded.Clear();
            }
        }
    }
}