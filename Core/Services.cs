//     
//  Core:Services.cs
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
    public sealed class DeclareServiceAttribute : Attribute
    {
        public readonly string Name;

        public DeclareServiceAttribute(string name)
        {
            Name = name;
        }
    }

    public sealed class ServiceDependencyAttribute : Attribute
    {
        public readonly string[] Dependencies;

        public ServiceDependencyAttribute(params string[] dependencies)
        {
            Dependencies = dependencies;
        }
    }

    [Serializable]
    public class ServiceManagerException : Exception
    {
        public ServiceManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [DeclareAssemblyReflectiveScanner]
    public sealed class Services : IAssemblyReflectiveScanner
    {
        private static readonly DisposeList Dispose = new DisposeList();
        private static readonly Dictionary<string, object> Ready = new Dictionary<string, object>();
        private static readonly Dictionary<string, Type> Providers = new Dictionary<string, Type>();
        private static readonly Dictionary<string, string[]> Dependencies = new Dictionary<string, string[]>();

        public void ProcessType(Type type)
        {
            if (type.IsDefined(typeof(DeclareServiceAttribute), false))
                Inject(type);
        }

        public static TI Get<TI>(string name)
        {
            try
            {
                if (!typeof(TI).IsAssignableFrom(Providers[name]))
                    throw new Exception($"Service Does Not Provide Interface {typeof(TI)}");
                if (!Ready.TryGetValue(name, out var service))
                    service = CreateService(name);
                return (TI) service;
            }
            catch (Exception e)
            {
                throw new ServiceManagerException("Cannot Create Service Instance", e);
            }
        }

        public static bool TryGet<TI>(string name, out TI ins)
        {
            try
            {
                ins = Get<TI>(name);
                return true;
            }
            catch (ServiceManagerException)
            {
                ins = default(TI);
                return false;
            }
        }

        private static void Inject(Type tp)
        {
            var name = tp.GetCustomAttribute<DeclareServiceAttribute>().Name;
            var dependents = tp.IsDefined(typeof(ServiceDependencyAttribute), false)
                ? tp.GetCustomAttribute<ServiceDependencyAttribute>().Dependencies
                : new string[0];
            Providers.Add(name, tp);
            Dependencies.Add(name, dependents);
        }

        private static object CreateService(string name)
        {
            foreach (var dependent in Dependencies[name])
                CreateService(dependent);
            var provider = Providers[name];
            var instance = Activator.CreateInstance(provider);
            if (typeof(IDisposable).IsAssignableFrom(Providers[name])) Dispose.List.Add((IDisposable) instance);
            Ready.Add(name, instance);
            return instance;
        }

        private class DisposeList
        {
            public readonly List<IDisposable> List = new List<IDisposable>();

            ~DisposeList()
            {
                foreach (var disposable in List)
                    disposable.Dispose();
            }
        }
    }
}