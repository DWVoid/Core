//     
//  Core:Protocols.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public interface IHandshake : IProtocolGroup
    {
        Task<KeyValuePair<string, uint>[]> Execute(IEndPoint conn);
    }

    public interface IHandshakeServerProtocol
    {
        void SetProtocolArray(List<Protocol> protocols);
    }

    public class Handshake: ProtocolGroup<Handshake>, IHandshake
    {
        public async Task<KeyValuePair<string, uint>[]> Execute(IEndPoint conn)
        {
            var session = Reply.AllocSession();
            using (var message = conn.CreateMessage(1))
            {
                message.Write(session.Key);
            }

            return (await session.Value).Get<KeyValuePair<string, uint>[]>();
        }

        protected class Server : GroupProtocolFixedLength, IHandshakeServerProtocol
        {
            private KeyValuePair<string, uint>[] _processed;

            public Server() : base(4)
            {
            }

            public override void HandleRequest(Session.Receive request)
            {
                var session = request.ReadUInt32();
                Reply.Send(request.Session, session, _processed);
            }

            public void SetProtocolArray(List<Protocol> protocols)
            {
                var current = 0;
                var reply = new KeyValuePair<string, uint>[protocols.Count];
                foreach (var protocol in protocols)
                    reply[current++] = new KeyValuePair<string, uint>(protocol.Name(), protocol.Id);
                _processed = reply;
            }
        }

        public override Protocol GetServerSide()
        {
            return new Server();
        }
    }
}