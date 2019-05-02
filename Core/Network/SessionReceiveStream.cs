//     
//  Core:SessionReceiveStream.cs
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

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public sealed partial class Session
    {
        private sealed class ReceiveStream : StreamNothing
        {
            private readonly Session session;
            private Stream stream;

            public ReceiveStream(Session s)
            {
                stream = s._ios;
                session = s;
            }

            public override bool CanRead => stream.CanRead;
            public override long Length => stream.Length;

            public override long Position
            {
                get => stream.Position;
                set => stream.Position = value;
            }

            internal async Task LoadExpected(int length)
            {
                if (length == 0) return;
                EnsureSize(length);
                await stream.ReadAsync(session._storage, 0, length);
                stream = session._buffer;
            }

            private void EnsureSize(int length)
            {
                ref var storage = ref session._storage;

                if (length > storage.Length)
                {
                    storage = new byte[1 << (int) System.Math.Ceiling(System.Math.Log(length) / System.Math.Log(2))];
                    session._buffer = new MemoryStream(storage, 0, storage.Length, false, true);
                }
                else
                {
                    session._buffer.Seek(0, SeekOrigin.Begin);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int ReadByte()
            {
                return stream.ReadByte();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Read(byte[] buffer, int begin, int count)
            {
                var end = begin + count;
                while (begin != end) begin += stream.Read(buffer, begin, end - begin);

                return count;
            }

            // ReSharper disable once TooManyArguments
            public override async Task<int> ReadAsync(byte[] buffer, int begin, int count,
                CancellationToken cancellationToken)
            {
                var end = begin + count;
                while (begin != end) begin += await stream.ReadAsync(buffer, begin, end - begin, cancellationToken);
                return count;
            }
        }
    }
}