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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public class Server : TcpListener
    {
        private readonly List<Protocol> protocols;

        public Server(int port) : base(IPAddress.Any, port)
        {
            protocols = new List<Protocol>();
            RegisterProtocol(new Reply());
            RegisterProtocol(new Handshake.Server(protocols));
        }

        public async Task RunAsync()
        {
            Boot();
            await ListenConnections();
        }

        public void RegisterProtocol(Protocol newProtocol)
        {
            protocols.Add(newProtocol);
        }

        public int CountConnections()
        {
            return ConnectionHost.CountConnections();
        }

        private void Boot()
        {
            AssignProtocolIdentifiers();
            Start();
        }

        private async Task ListenConnections()
        {
            while (Active)
                try
                {
                    var client = await AcceptTcpClientAsync();
                    var connection = new  ConnectionHost.StreamConnection(client);
                    connection.SetTcpStream();
                    ConnectionHost.Add(connection, protocols);
                }
                catch
                {
                    // ignored
                }
        }

        private void AssignProtocolIdentifiers()
        {
            var current = 0u;
            foreach (var protocol in protocols)
                protocol.Id = current++;
        }
    }
}