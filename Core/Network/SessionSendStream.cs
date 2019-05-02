//     
//  Core:SessionSendStream.cs
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

namespace Akarin.Network
{
    public sealed partial class Session
    {
        private sealed class SendStream : StreamNothing
        {
            private const int BufferSize = 4096;

            private readonly Stream _stream;

            private byte[] _current;

            private Task _lastWrite;

            private int _used;

            internal SendStream(Stream stream)
            {
                _current = GetNextBuffer();
                _stream = stream;
            }

            public override bool CanWrite => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void WriteByte(byte value)
            {
                if (_used == BufferSize)
                    Flush0();
                _current[_used++] = value;
            }

            public override void Flush()
            {
                Flush0();
                _lastWrite?.Wait();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > BufferSize)
                {
                    WriteDirect(buffer, offset, count);
                    return;
                }

                CopyToBuffer(buffer, offset, count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CopyToBuffer(byte[] buffer, int offset, int count)
            {
                var remain = BufferSize - _used;
                if (remain >= count)
                {
                    Array.Copy(buffer, offset, _current, _used, count);
                    _used += count;
                }
                else
                {
                    Array.Copy(buffer, offset, _current, _used, remain);
                    _used = BufferSize;
                    Flush0();
                    _used = count - remain;
                    Array.Copy(buffer, offset + remain, _current, 0, _used);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static byte[] GetNextBuffer()
            {
                return new byte[BufferSize];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteDirect(byte[] buffer, int offset, int count)
            {
                Flush0();
                Write0(new ArraySegment<byte>(buffer, offset, count));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Flush0()
            {
                if (_used > 0)
                {
                    Write0(new ArraySegment<byte>(_current, 0, _used));
                    _current = GetNextBuffer();
                    _used = 0;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Write0(ArraySegment<byte> target)
            {
                _lastWrite = Write1(target, _lastWrite);
            }

            private async Task Write1(ArraySegment<byte> target, Task last)
            {
                if (last != null) await last;

                _stream.Write(target.Array, target.Offset, target.Count);
            }
        }
    }
}