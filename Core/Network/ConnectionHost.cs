//     
//  Core:ConnectionHost.cs
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

namespace Akarin.Network
{
    internal static partial class ConnectionHost
    {
        private const double UtilizationThreshold = 0.25;
        private static int _connectionCounter;
        private static List<EndPoint> _connections;

        static ConnectionHost()
        {
            _connections = new List<EndPoint>();
        }

        private static void SweepInvalidConnectionsIfNecessary()
        {
            var utilization = (double) _connectionCounter / _connections.Count;
            if (utilization < UtilizationThreshold)
                SweepInvalidConnections();
        }

        private static void SweepInvalidConnections()
        {
            var swap = new List<EndPoint>();
            foreach (var hd in _connections)
                if (hd.Valid)
                    swap.Add(hd);
            _connections = swap;
        }

        internal static IEndPoint Add(StreamConnection conn, List<Protocol> protocols)
        {
            var connect = new EndPoint(conn, protocols);
            _connections.Add(connect);
            return connect;
        }

        public static int CountConnections()
        {
            return _connectionCounter;
        }
    }
}