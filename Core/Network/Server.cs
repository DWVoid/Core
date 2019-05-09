//     
//  Core:Server.cs
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public class ServerCreateInfo
    {
        public int Port;
        public IHandshake HandshakeGroup;
        public string[] ProtocolGroups;
        public X509Certificate ServerCert = null;
    }
   
    public class Server : TcpListener
    {
        private readonly List<Protocol> _protocols;
        private readonly X509Certificate _certificate;

        public Server(ServerCreateInfo create) : base(IPAddress.Any, create.Port)
        {
            _certificate = create.ServerCert;
            _protocols = new List<Protocol> {new Reply(), create.HandshakeGroup.GetServerSide()}
                .Concat(ProtocolGroupDiscoverer.GetServerSide(create.ProtocolGroups))
                .ToList();
            (_protocols[1] as IHandshakeServerProtocol)?.SetProtocolArray(_protocols);
        }

        public Task RunAsync()
        {
            Boot();
            return ListenConnections();
        }

        private void Boot()
        {
            AssignProtocolIdentifiers();
            Start();
        }

        private async Task ListenConnections()
        {
            while (Active)
            {
                try
                {
                    var client = await AcceptTcpClientAsync();
                    EnableClient(client);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void EnableClient(TcpClient client)
        {
            var connection = new ConnectionHost.StreamConnection(client);
            if (_certificate != null)
            {
                connection.SetSslServerStream(_certificate);
            }
            else
            {
                connection.SetTcpStream();
            }

            ConnectionHost.Add(connection, _protocols);
        }

        private void AssignProtocolIdentifiers()
        {
            var current = 0u;
            foreach (var protocol in _protocols)
                protocol.Id = current++;
        }
    }
}