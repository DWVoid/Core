//     
//  Core:SessionSendInterface.cs
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Akarin.Network
{
    public sealed partial class Session
    {
        public sealed class Send : BinaryWriter, IDisposable
        {
            private readonly Session session;

            internal Send(Session session, uint protocol)
            {
                this.session = session;
                OutStream = new SendStream(this.session._ios);
                session._writeLock.EnterWriteLock();
                Write(0x4352574E);
                Write(protocol);
            }

            void IDisposable.Dispose()
            {
                ReleaseUnmanagedResources();
                base.Dispose();
                GC.SuppressFinalize(this);
            }

            ~Send()
            {
                ReleaseUnmanagedResources();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write(ArraySegment<byte> bytes)
            {
                Debug.Assert(bytes.Array != null, "bytes.Array != null");
                Write(bytes.Array, bytes.Offset, bytes.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteObject<T>(T obj)
            {
                MessagePack.MessagePackSerializer.Serialize(OutStream, obj);
            }

            private void ReleaseUnmanagedResources()
            {
                OutStream.Flush();
                session._writeLock.ExitWriteLock();
            }
        }
    }
}