using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class TcpStateCollector
    {
        public void Collect(HostMetrics metrics)
        {
            var states = new Dictionary<TcpState, int>();

            CountStates(states);

            metrics.TcpStates = states;
        }

        private void CountStates(IDictionary<TcpState, int> states)
        {
#if NET6_0_OR_GREATER

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IterateOverTcpConnectionsStates(state =>
                {
                    if (state is not TcpState.Listen)
                        Increment(states, state);
                });
            }
            else
            {
                CountStatesUsingGlobalProperties();
            }
#else
            CountStatesUsingGlobalProperties();
#endif

            void CountStatesUsingGlobalProperties()
            {
                foreach (var tcpConnection in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
                {
                    Increment(states, tcpConnection.State);
                }
            }
        }

#if NET6_0_OR_GREATER
        // Copy pasted from System.Net.NetworkInformation
        private static unsafe void IterateOverTcpConnectionsStates(Action<TcpState> handle)
        {
            var size = 0U;
            uint result;

            #region ipv4

            if (Socket.OSSupportsIPv4)
            {
                result = Interop.IpHlpApi.GetTcpTable(IntPtr.Zero, ref size, true);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    var buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetTcpTable(buffer, ref size, true);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            ref readonly var tcpTableInfo = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcpTable>(span);

                            if (tcpTableInfo.numberOfEntries > 0)
                            {
                                span = span[sizeof(Interop.IpHlpApi.MibTcpTable)..];

                                for (var i = 0; i < tcpTableInfo.numberOfEntries; i++)
                                {
                                    var row = MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcpRow>(span);
                                    handle(row.state);
                                    span = span[sizeof(Interop.IpHlpApi.MibTcpRow)..];
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            #endregion

            #region ipv6

            if (Socket.OSSupportsIPv6)
            {
                size = 0;
                result = Interop.IpHlpApi.GetExtendedTcpTable(IntPtr.Zero,
                    ref size,
                    true,
                    (uint)AddressFamily.InterNetworkV6,
                    Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll,
                    0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    var buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedTcpTable(buffer,
                            ref size,
                            true,
                            (uint)AddressFamily.InterNetworkV6,
                            Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll,
                            0);
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            ref readonly var tcpTable6OwnerPid = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcp6TableOwnerPid>(span);

                            if (tcpTable6OwnerPid.numberOfEntries > 0)
                            {
                                span = span[sizeof(Interop.IpHlpApi.MibTcp6TableOwnerPid)..];

                                for (var i = 0; i < tcpTable6OwnerPid.numberOfEntries; i++)
                                {
                                    var row = MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcp6RowOwnerPid>(span);
                                    handle(row.state);
                                    span = span[sizeof(Interop.IpHlpApi.MibTcp6RowOwnerPid)..];
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            #endregion
        }
#endif

        private static void Increment<T>(IDictionary<T, int> counter, T key)
        {
            if (!counter.ContainsKey(key))
                counter[key] = 0;
            counter[key]++;
        }
    }
}