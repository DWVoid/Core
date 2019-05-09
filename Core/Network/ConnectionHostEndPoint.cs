//     
//  Core:ConnectionHostEndPoint.cs
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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Akarin.Network
{
    public interface IEndPoint
    {
        bool Valid { get; }
        void Close();
        Session.Send CreateMessage(uint protocol);
    }

    internal static partial class ConnectionHost
    {
        internal sealed class StreamConnection : IDisposable
        {
            private readonly TcpClient _client;

            internal StreamConnection(TcpClient client)
            {
                _client = client;
            }

            internal Stream Stream { get; private set; }

            internal bool Connected => _client.Connected;

            public void Dispose()
            {
                Stream?.Dispose();
                _client?.Dispose();
            }

            internal void SetTcpStream()
            {
                Stream = _client.GetStream();
            }

            internal void SetSslClientStream(string serverName)
            {
                var stream = new SslStream(_client.GetStream(), false,
                    (sender, certificate, chain, sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None, null);
                stream.AuthenticateAsClient(serverName);
                Stream = stream;
            }
            
            internal void SetSslServerStream(X509Certificate cert)
            {
                var sslStream = new SslStream(_client.GetStream(), false);
                sslStream.AuthenticateAsServer(cert, false, SslProtocols.Default, true);
                Stream = sslStream;
            }

            internal void Close()
            {
                Stream.Close();
                _client.Close();
            }
        }

        internal sealed class EndPoint : IEndPoint, IDisposable
        {
            private readonly StreamConnection _connection;
            private readonly Task _finalize;
            private readonly List<Protocol> _protocols;
            internal readonly Session Session;

            internal EndPoint(StreamConnection connection, List<Protocol> protocols)
            {
                _protocols = protocols;
                _connection = connection;
                Session = new Session(_connection.Stream);
                _finalize = Start();
            }

            public void Dispose()
            {
                Close();
                _connection?.Dispose();
                Session?.Dispose();
            }

            public bool Valid { get; private set; }

            public void Close()
            {
                Valid = false;
                _finalize.Wait();
                _finalize?.Dispose();
            }

            public Session.Send CreateMessage(uint protocol)
            {
                return Session.CreateMessage(protocol);
            }

            private async Task Start()
            {
                Valid = true;
                Interlocked.Increment(ref _connectionCounter);
                while (Valid && _connection.Connected)
                    try
                    {
                        var message = Session.WaitMessage();
                        await ProcessRequest(await message.Wait(), message);
                    }
                    catch (Exception e)
                    {
                        if (_connection.Connected) LogPort.Debug($"Encountering Exception {e}");
                    }

                CleanUp();
            }

            private async Task ProcessRequest(int protocol, Session.Receive message)
            {
                var handle = _protocols[protocol];
                await message.LoadExpected(handle.Expecting);
                handle.HandleRequest(message);
            }

            private void CleanUp()
            {
                if (!Valid) return;
                Valid = false;
                _connection.Close();
                Interlocked.Decrement(ref _connectionCounter);
                SweepInvalidConnectionsIfNecessary();
            }
        }
    }
}