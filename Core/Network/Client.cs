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
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public sealed class Client : IDisposable
    {
        private readonly IEndPoint _endPoint;
        private readonly List<Protocol> protocols;

        public Client(string address, int port)
        {
            var client = new TcpClient(address, port);
            var connection = new ConnectionHost.StreamConnection(client);
            connection.SetTcpStream();
            protocols = new List<Protocol>();
            RegisterProtocol(new Reply());
            //RegisterProtocol(new Handshake.Client());
            _endPoint = ConnectionHost.Add(connection, protocols);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Client()
        {
            ReleaseUnmanagedResources();
        }

        public void RegisterProtocol(Protocol newProtocol)
        {
            protocols.Add(newProtocol);
        }

        public async Task HandShake()
        {
            var skvm = new Dictionary<string, Protocol>();
            foreach (var protocol in protocols)
                skvm.Add(protocol.Name(), protocol);
            var reply = await Handshake.Get(_endPoint);
            foreach (var entry in reply)
                skvm[entry.Key].Id = entry.Value;
            protocols.Sort(ProtocolSorter);
        }

        public Session.Send CreateMessage(uint protocol)
        {
            return _endPoint.CreateMessage(protocol);
        }

        public void Close()
        {
            _endPoint.Close();
        }

        private static int ProtocolSorter(Protocol x, Protocol y)
        {
            return Comparer<uint>.Default.Compare(x.Id, y.Id);
        }

        private void ReleaseUnmanagedResources()
        {
            Close();
        }
    }
}