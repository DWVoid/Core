//     
//  Core:SessionStreamNothing.cs
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

namespace Akarin.Network
{
    public sealed partial class Session
    {
        private abstract class StreamNothing : Stream
        {
            private void ThrowException()
            {
                throw new Exception("Not Allowed");
            }

            private T ThrowException<T>()
            {
                throw new Exception("Not Allowed");
            }

            public override void Flush()
            {
                ThrowException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ThrowException<int>();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return ThrowException<long>();
            }

            public override void SetLength(long value)
            {
                ThrowException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ThrowException();
            }

            public override int ReadByte()
            {
                return ThrowException<int>();
            }

            public override void WriteByte(byte value)
            {
                ThrowException();
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => 0;
            public override long Position { get; set; } = 0;
        }
    }
}