//     
//  Core:ProtocolReply.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace Akarin.Network
{
    public sealed class Reply : Protocol
    {
        private static int _idTop;
        private static readonly ConcurrentQueue<uint> SessionIds = new ConcurrentQueue<uint>();

        private static readonly ConcurrentDictionary<uint, TaskCompletionSource<Payload>> Sessions =
            new ConcurrentDictionary<uint, TaskCompletionSource<Payload>>();

        public override string Name()
        {
            return "Reply";
        }

        public override void HandleRequest(Session.Receive request)
        {
            var session = request.ReadUInt32();
            var length = request.ReadUInt32();
            var dataSegment = new byte[length];
            request.Read(dataSegment, 0, dataSegment.Length);
            SessionDispatch(session, dataSegment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Send<T>(Session dialog, uint session, T payload)
        {
            Send(dialog, session, MessagePackSerializer.SerializeUnsafe(payload));
        }

        public static void Send(Session dialog, uint session, ArraySegment<byte> payload)
        {
            using (var message = dialog.CreateMessage(0))
            {
                message.Write(session);
                message.Write((uint) payload.Count);
                message.Write(payload);
            }
        }

        public static KeyValuePair<uint, Task<Payload>> AllocSession()
        {
            if (!SessionIds.TryDequeue(out var newId))
                newId = (uint) (Interlocked.Increment(ref _idTop) - 1);

            var completionSource = new TaskCompletionSource<Payload>();
            while (!Sessions.TryAdd(newId, completionSource))
            {
            }

            return new KeyValuePair<uint, Task<Payload>>(newId, completionSource.Task);
        }

        private static void SessionDispatch(uint sessionId, byte[] dataSegment)
        {
            TaskCompletionSource<Payload> completion;
            while (!Sessions.TryRemove(sessionId, out completion))
            {
            }

            completion.SetResult(new Payload {Raw = dataSegment});
            SessionIds.Enqueue(sessionId);
        }

        public struct Payload
        {
            public byte[] Raw;

            public T Get<T>()
            {
                return MessagePackSerializer.Deserialize<T>(Raw);
            }
        }
    }
}