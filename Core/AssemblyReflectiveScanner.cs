//     
//  Core:AssemblyReflectiveScanner.cs
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
    public sealed class DeclareNeWorldAssemblyAttribute : Attribute
    {
        internal readonly AssemblyScanPolicy Policy;

        public DeclareNeWorldAssemblyAttribute(AssemblyScanPolicy policy = AssemblyScanPolicy.Default)
        {
            Policy = policy;
        }
    }

    public enum AssemblyScanPolicy
    {
        PublicOnly,
        All,
        Default = PublicOnly
    }

    public sealed class DeclareAssemblyReflectiveScannerAttribute : Attribute
    {
    }

    public interface IAssemblyReflectiveScanner
    {
        void ProcessType(Type type);
    }

    internal static class AssemblyReflectiveScanner
    {
        // Only for conflict resolve for multi-thread load
        private static HashSet<AssemblyName> _processed = new HashSet<AssemblyName>();
        private static readonly object ProcessLock = new object();
        private static readonly List<IAssemblyReflectiveScanner> Scanners = new List<IAssemblyReflectiveScanner>();
        private static readonly List<Assembly> Scanned = new List<Assembly>();

        internal static void UpdateDomainAssemblies()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoadServiceRegisterAgent;
            var snapshot = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in snapshot)
            {
                if (!CheckIfAssemblyProcessed(assembly))
                {
                    ScanAssembly(assembly);
                }
            }

            UpdateDomainAssembliesProcessKeepUp();
        }

        private static void UpdateDomainAssembliesProcessKeepUp()
        {
            lock (ProcessLock)
            {
                _processed = null;
                lock (Scanned)
                {
                    foreach (var assembly in Scanned)
                    {
                        ProcessNewAssembly(assembly);
                    }
                }
            }
        }

        private static bool CheckIfAssemblyProcessed(Assembly assembly)
        {
            lock (ProcessLock)
            {
                return _processed != null && (bool) _processed?.Contains(assembly.GetName());
            }
        }

        private static void OnAssemblyLoadServiceRegisterAgent(object sender, AssemblyLoadEventArgs args)
        {
            ScanAssembly(args.LoadedAssembly);
        }

        private static void ScanForAssemblyScanners(Assembly assembly)
        {
            var allowPrivate = GetAssemblyScanPolicy(assembly) == AssemblyScanPolicy.All;
            foreach (var type in assembly.DefinedTypes)
            {
                if ((type.IsPublic || allowPrivate) && IsScannerType(type))
                {
                    InitializeScanner(type);
                }
            }
        }

        private static bool IsScannerType(Type type)
        {
            return type.IsDefined(typeof(DeclareAssemblyReflectiveScannerAttribute), false) &&
                   typeof(IAssemblyReflectiveScanner).IsAssignableFrom(type);
        }

        private static void InitializeScanner(Type type)
        {
            var currentScanner = (IAssemblyReflectiveScanner) Activator.CreateInstance(type);
            lock (Scanners)
            {
                Scanners.Add(currentScanner);
            }

            lock (ProcessLock)
            {
                if (_processed != null) return;
                ProcessPastAssemblies(currentScanner);
            }
        }

        private static void ProcessPastAssemblies(IAssemblyReflectiveScanner currentScanner)
        {
            lock (Scanned)
            {
                foreach (var assembly in Scanned)
                {
                    ProcessPastAssembly(currentScanner, assembly);
                }
            }
        }

        private static void ProcessPastAssembly(IAssemblyReflectiveScanner currentScanner, Assembly assembly)
        {
            var allowPrivate = GetAssemblyScanPolicy(assembly) == AssemblyScanPolicy.All;
            foreach (var target in assembly.DefinedTypes)
            {
                if (target.IsPublic || allowPrivate)
                {
                    currentScanner.ProcessType(target);
                }
            }
        }

        private static AssemblyScanPolicy GetAssemblyScanPolicy(Assembly assembly)
        {
            return assembly.GetCustomAttribute<DeclareNeWorldAssemblyAttribute>().Policy;
        }

        private static void ScanAssembly(Assembly assembly)
        {
            lock (ProcessLock)
            {
                _processed?.Add(assembly.GetName(true));
            }

            if (!assembly.IsDefined(typeof(DeclareNeWorldAssemblyAttribute), false)) return;

            ScanForAssemblyScanners(assembly);

            lock (Scanned)
            {
                Scanned.Add(assembly);
            }

            lock (ProcessLock)
            {
                if (_processed == null)
                {
                    ProcessNewAssembly(assembly);
                }
            }
        }

        private static void ProcessNewAssembly(Assembly assembly)
        {
            var allowPrivate = GetAssemblyScanPolicy(assembly) == AssemblyScanPolicy.All;
            foreach (var target in assembly.DefinedTypes)
            {
                if (target.IsPublic || allowPrivate)
                {
                    ProcessNewAssemblyType(target);
                }
            }
        }

        private static void ProcessNewAssemblyType(Type target)
        {
            lock (Scanners)
            {
                foreach (var currentScanner in Scanners)
                {
                    currentScanner.ProcessType(target);
                }
            }
        }
    }
}