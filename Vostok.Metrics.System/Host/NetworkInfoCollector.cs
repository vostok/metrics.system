using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Vostok.Metrics.System.Host
{
    internal class NetworkInfoCollector
    {
        public void Collect(HostMetrics metrics)
        {
            CollectTcpStateMetrics(metrics);
            // CollectNetworkUsageMetrics(metrics);
        }

        private void CollectTcpStateMetrics(HostMetrics metrics)
        {
            var states = new Dictionary<TcpState, int>();

            foreach (var tcpConnection in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
            {
                if (!states.ContainsKey(tcpConnection.State))
                    states[tcpConnection.State] = 0;
                states[tcpConnection.State]++;
            }

            metrics.TcpStateMetrics = states;
        }

        private void CollectNetworkUsageMetrics(HostMetrics metrics)
        {
            throw new NotImplementedException();
        }
    }
}