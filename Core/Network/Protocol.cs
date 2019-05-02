//     
//  Core:Protocol.cs
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

namespace Akarin.Network
{
    public abstract class Protocol
    {
        public uint Id { get; set; }

        public int Expecting { get; protected set; }

        public abstract string Name();

        public abstract void HandleRequest(Session.Receive request);
    }

    public abstract class FixedLengthProtocol : Protocol
    {
        protected FixedLengthProtocol(int length)
        {
            Expecting = length;
        }
    }

    public abstract class StubProtocol : Protocol
    {
        public override void HandleRequest(Session.Receive request)
        {
        }
    }
}