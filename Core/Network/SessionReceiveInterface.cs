//     
//  Core:SessionReceiveInterface.cs
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MessagePack;

namespace Akarin.Network
{
    public sealed partial class Session
    {
        public sealed class Receive : BinaryReader
        {
            internal Receive(Session s) : base(new ReceiveStream(s))
            {
                Session = s;
            }

            public Session Session { get; }

            internal async Task<int> Wait()
            {
                var head = new byte[4];
                await BaseStream.ReadAsync(head, 0, 4);
                if (CheckHeaderMark(head))
                    throw new Exception("Bad Package Received");
                return (int) ReadUInt32();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool CheckHeaderMark(byte[] head)
            {
                return ((head[0] << 24) | (head[1] << 16) | (head[2] << 8) | head[3]) != 0x4E575243;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Read<T>()
            {
                return MessagePackSerializer.Deserialize<T>(BaseStream);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Task LoadExpected(int length)
            {
                return ((ReceiveStream) BaseStream).LoadExpected(length);
            }
        }
    }
}