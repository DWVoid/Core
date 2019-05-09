//     
//  Core:ProtocolGroup.cs
//  Created on 2019/05/02 21:18
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
using System.Linq;
using System.Reflection;

namespace Akarin.Network
{
    public interface IProtocolGroup
    {
        Protocol GetClientSide();

        Protocol GetServerSide();
    }

    public abstract class AProtocolGroup : IProtocolGroup
    {
        internal abstract void SetProtocolName(string name);

        public abstract Protocol GetClientSide();

        public abstract Protocol GetServerSide();
    }

    public abstract class AStaticInvoke
    {
        internal IEndPoint Endpoint { private get; set; }
        internal uint Id { private get; set; }
        internal abstract string Name { get; }

        protected Session.Send CreateMessage()
        {
            return Endpoint.CreateMessage(Id);
        }
    }

    public class ProtocolGroup<T> : AProtocolGroup
    {
        private static string ProtocolName;

        public abstract class GroupProtocol : Protocol
        {
            public override string Name()
            {
                return ProtocolName;
            }
        }

        public abstract class GroupProtocolTyped<TU> : GroupProtocol
        {
            protected abstract void HandleRequestObject(TU obj);

            public sealed override void HandleRequest(Session.Receive request)
            {
                HandleRequestObject(MessagePack.MessagePackSerializer.Deserialize<TU>(request.BaseStream));
            }
        }
        
        public abstract class GroupProtocolFixedLength : FixedLengthProtocol
        {
            public override string Name()
            {
                return ProtocolName;
            }

            protected GroupProtocolFixedLength(int length) : base(length)
            {
            }
        }

        public abstract class GroupProtocolTypedFixedLength<TU> : GroupProtocolFixedLength
        {
            protected abstract void HandleRequestObject(TU obj);

            public sealed override void HandleRequest(Session.Receive request)
            {
                HandleRequestObject(MessagePack.MessagePackSerializer.Deserialize<TU>(request.BaseStream));
            }

            protected GroupProtocolTypedFixedLength(int length) : base(length)
            {
            }
        }

        private class Stub : GroupProtocol
        {
            public override void HandleRequest(Session.Receive request)
            {
            }
        }

        internal override void SetProtocolName(string name)
        {
            ProtocolName = name;
        }

        public class StaticInvoke : AStaticInvoke
        {
            internal override string Name => ProtocolName;
        }

        public override Protocol GetClientSide() => new Stub();
        
        public override Protocol GetServerSide() => new Stub();
    }

    public sealed class DefineProtocolGroupAttribute : Attribute
    {
        public string Name { get; set; }
        
        public Version Current { get; set; }

        public Version Conflict { get; set; }

        public string ProtocolGroup { get; set; }
    }

    [DeclareAssemblyReflectiveScanner]
    internal class ProtocolGroupDiscoverer : IAssemblyReflectiveScanner
    {
        private static Dictionary<string, List<AProtocolGroup>> groups = new Dictionary<string, List<AProtocolGroup>>();

        public void ProcessType(Type type)
        {
            if (IsProtocolGroup(type))
            {
                var info = type.GetCustomAttribute<DefineProtocolGroupAttribute>();
                var group = Activator.CreateInstance(type) as AProtocolGroup;
                group.SetProtocolName(info.Name);
                if (!groups.ContainsKey(info.ProtocolGroup))
                {
                    groups.Add(info.ProtocolGroup, new List<AProtocolGroup>());
                }

                groups[info.ProtocolGroup].Add(group);
            }
        }

        internal static List<Protocol> GetServerSide(params string[] names)
        {
            return (from name in names from @group in groups[name] select @group.GetServerSide()).ToList();
        }

        internal static List<Protocol> GetClientSide(params string[] names)
        {
            return (from name in names from @group in groups[name] select @group.GetClientSide()).ToList();
        }

        private static bool IsProtocolGroup(Type type)
        {
            return typeof(AProtocolGroup).IsAssignableFrom(type) &&
                   type.IsDefined(typeof(DefineProtocolGroupAttribute), false);
        }
    }
}
