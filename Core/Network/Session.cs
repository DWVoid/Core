//     
//  Core:Session.cs
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
using System.Threading;

namespace Akarin.Network
{
    public sealed partial class Session : IDisposable
    {
        private readonly Stream _ios;
        private readonly ReaderWriterLockSlim _writeLock = new ReaderWriterLockSlim();
        private MemoryStream _buffer;
        private byte[] _storage = new byte[8192];

        internal Session(Stream stream)
        {
            _ios = stream;
            _buffer = new MemoryStream(_storage, 0, _storage.Length, false, true);
        }

        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        ~Session()
        {
            ReleaseResources();
        }

        internal Receive WaitMessage()
        {
            return new Receive(this);
        }

        internal Send CreateMessage(uint protocol)
        {
            return new Send(this, protocol);
        }

        private void ReleaseResources()
        {
            _ios.Close();
            _ios?.Dispose();
            _buffer?.Dispose();
        }
    }
}