//     
//  Core:EventBus.cs
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
using System.Threading;

namespace Akarin
{
    public class DeclareBusEventHandlerAttribute : Attribute
    {
    }

    public static class EventBus
    {
        public delegate void EventHandler<in T>(object sender, T payload);

        private static readonly Dictionary<Type, object> EventHandlers = new Dictionary<Type, object>();

        public static void Add<T>(EventHandler<T> handler)
        {
            var slot = GetOrCreateSlot<T>();
            slot.Rwl.EnterWriteLock();
            slot.Handlers += handler;
            slot.Rwl.ExitWriteLock();
        }

        private static Slot<T> GetOrCreateSlot<T>()
        {
            Slot<T> slot;
            lock (EventHandlers)
            {
                if (EventHandlers.TryGetValue(typeof(T), out var value))
                {
                    slot = (Slot<T>) value;
                }
                else
                {
                    slot = new Slot<T>();
                    EventHandlers.Add(typeof(T), slot);
                }
            }

            return slot;
        }

        private static ISlot GetOrCreateSlot(Type type)
        {
            ISlot slot;
            lock (EventHandlers)
            {
                if (EventHandlers.TryGetValue(type, out var value))
                {
                    slot = (ISlot) value;
                }
                else
                {
                    slot = (ISlot) Activator.CreateInstance(typeof(Slot<>).MakeGenericType(type));
                    EventHandlers.Add(type, slot);
                }
            }

            return slot;
        }

        public static void Remove<T>(EventHandler<T> handler)
        {
            Slot<T> slot;
            lock (EventHandlers)
            {
                if (EventHandlers.TryGetValue(typeof(T), out var value))
                    slot = (Slot<T>) value;
                else
                    return;
            }

            slot.Rwl.EnterWriteLock();
            slot.Handlers -= handler;
            slot.Rwl.ExitWriteLock();
        }

        public static void AddCollection(object obj)
        {
            ProcessCollection(obj, true);
        }

        public static void RemoveCollection(object obj)
        {
            ProcessCollection(obj, false);
        }

        private static void ProcessCollection(object obj, bool add)
        {
            foreach (var method in obj.GetType().GetMethods())
                if (method.IsDefined(typeof(DeclareBusEventHandlerAttribute), true))
                {
                    var payload = method.GetParameters();
                    if (payload.Length == 2)
                    {
                        var payloadType = payload[payload.Length - 1].ParameterType;
                        var handlerType = typeof(EventHandler<>).MakeGenericType(payloadType);
                        var del = method.IsStatic
                            ? Delegate.CreateDelegate(handlerType, method)
                            : Delegate.CreateDelegate(handlerType, obj, method);
                        if (add)
                            GetOrCreateSlot(payloadType).Add(del);
                        else
                            GetOrCreateSlot(payloadType).Remove(del);
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Excepting Arguments (System.Object, T) But Got {payload.Length} at Handler {method}" +
                            ", Stopping. Note that Previously Added Handlers will NOT be Removed");
                    }
                }
        }

        public static void Broadcast<T>(object sender, T payload)
        {
            Slot<T> slot = null;
            lock (EventHandlers)
            {
                if (EventHandlers.TryGetValue(typeof(T), out var value))
                    slot = (Slot<T>) value;
            }

            slot?.Invoke(sender, payload);
        }

        [DeclareAssemblyReflectiveScanner]
        private sealed class GlobalHandlerClassDetector : IAssemblyReflectiveScanner
        {
            public void ProcessType(Type type)
            {
                if (type.IsDefined(typeof(DeclareGlobalBusEventHandlerClassAttribute), false))
                    AddCollection(Activator.CreateInstance(type));
            }
        }

        private interface ISlot
        {
            void Add(Delegate handler);
            void Remove(Delegate handler);
        }

        private class Slot<T> : ISlot
        {
            public readonly ReaderWriterLockSlim Rwl = new ReaderWriterLockSlim();

            public void Add(Delegate handler)
            {
                Rwl.EnterWriteLock();
                typeof(Slot<T>).GetEvents()[0].AddMethod.Invoke(this, new object[] {handler});
                Rwl.ExitWriteLock();
            }

            public void Remove(Delegate handler)
            {
                Rwl.EnterWriteLock();
                typeof(Slot<T>).GetEvents()[0].RemoveMethod.Invoke(this, new object[] {handler});
                Rwl.ExitWriteLock();
            }

            public event EventHandler<T> Handlers;

            public void Invoke(object sender, T payload)
            {
                Rwl.EnterReadLock();
                Handlers?.Invoke(sender, payload);
                Rwl.ExitReadLock();
            }
        }
    }

    public sealed class DeclareGlobalBusEventHandlerClassAttribute : Attribute
    {
    }
}