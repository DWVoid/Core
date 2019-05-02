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
    [DefineProtocolGroup(Name = "Handshake", ProtocolGroup = "Ssl.Core")]
    public class Handshake: ProtocolGroup<Handshake>
    {
        internal static async Task<KeyValuePair<string, uint>[]> Get(IEndPoint conn)
        {
            var session = Reply.AllocSession();
            using (var message = conn.CreateMessage(1))
            {
                message.Write(session.Key);
            }

            return (await session.Value).Get<KeyValuePair<string, uint>[]>();
        }

        public class Server : GroupProtocolFixedLength
        {
            private readonly List<Protocol> protocols;

            public Server(List<Protocol> protocols) : base(4)
            {
                this.protocols = protocols;
            }

            public override void HandleRequest(Session.Receive request)
            {
                var session = request.ReadUInt32();
                var current = 0;
                var reply = new KeyValuePair<string, uint>[protocols.Count];
                foreach (var protocol in protocols)
                    reply[current++] = new KeyValuePair<string, uint>(protocol.Name(), protocol.Id);
                Reply.Send(request.Session, session, reply);
            }
        }
    }
}