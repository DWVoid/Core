//     
//  Core:Client.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public class ClientCreateInfo
    {
        public string Address;
        public IHandshake HandshakeGroup;
        public int Port;
        public string[] ProtocolGroups;
        public string SecureServerName = null;
    }

    public sealed class Client : IDisposable
    {
        private readonly IEndPoint _endPoint;
        private readonly List<Protocol> _protocols;

        private Client(ClientCreateInfo create)
        {
            var client = new TcpClient(create.Address, create.Port);
            var connection = new ConnectionHost.StreamConnection(client);
            if (create.SecureServerName == null)
                connection.SetTcpStream();
            else
                connection.SetSslClientStream(create.SecureServerName);

            _protocols = new List<Protocol> {new Reply(), create.HandshakeGroup.GetClientSide()}
                .Concat(ProtocolGroupDiscoverer.GetClientSide(create.ProtocolGroups))
                .ToList();
            _endPoint = ConnectionHost.Add(connection, _protocols);
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public static async Task<Client> CreateClient(ClientCreateInfo create)
        {
            var ret = new Client(create);
            await ret.HandShake(create.HandshakeGroup);
            return ret;
        }

        ~Client()
        {
            Close();
        }

        private async Task HandShake(IHandshake handshake)
        {
            var skvm = new Dictionary<string, Protocol>();
            foreach (var protocol in _protocols)
                skvm.Add(protocol.Name(), protocol);
            var reply = await handshake.Execute(_endPoint);
            foreach (var entry in reply)
                skvm[entry.Key].Id = entry.Value;
            _protocols.Sort((x, y) => Comparer<uint>.Default.Compare(x.Id, y.Id));
        }

        public Session.Send CreateMessage(uint protocol)
        {
            return _endPoint.CreateMessage(protocol);
        }

        public void Close()
        {
            _endPoint.Close();
        }
    }
}