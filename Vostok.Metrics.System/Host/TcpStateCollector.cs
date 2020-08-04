using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Vostok.Metrics.System.Host
{
    internal class TcpStateCollector
    {
        public void Collect(HostMetrics metrics)
        {
            var states = new Dictionary<TcpState, int>();

            foreach (var tcpConnection in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
            {
                if (!states.ContainsKey(tcpConnection.State))
                    states[tcpConnection.State] = 0;
                states[tcpConnection.State]++;
            }

            metrics.TcpStates = states;
        }
    }
}