#if NET6_0_OR_GREATER
using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Vostok.Metrics.System.Helpers;

// Copy pasted from System.Net.NetworkInformation
internal static class Interop
{
    private static class Libraries
    {
        public const string IpHlpApi = "iphlpapi.dll";
    }

    internal static class IpHlpApi
    {
        public const int ERROR_INSUFFICIENT_BUFFER = 0x007A;
        public const int ERROR_SUCCESS = 0x0;
        public const int ERROR_NO_DATA = 0xE8;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcpTable
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcpRow
        {
            internal TcpState state;
            internal uint localAddr;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
            internal uint remoteAddr;
            internal byte remotePort1;
            internal byte remotePort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreRemotePort3;
            internal byte ignoreRemotePort4;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcp6TableOwnerPid
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MibTcp6RowOwnerPid
        {
            internal fixed byte localAddr[16];
            internal uint localScopeId;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
            internal fixed byte remoteAddr[16];
            internal uint remoteScopeId;
            internal byte remotePort1;
            internal byte remotePort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreRemotePort3;
            internal byte ignoreRemotePort4;
            internal TcpState state;
            internal uint owningPid;

            internal ReadOnlySpan<byte> localAddrAsSpan => MemoryMarshal.CreateSpan(ref localAddr[0], 16);
            internal ReadOnlySpan<byte> remoteAddrAsSpan => MemoryMarshal.CreateSpan(ref remoteAddr[0], 16);
        }

        internal enum TcpTableClass
        {
            TcpTableBasicListener = 0,
            TcpTableBasicConnections = 1,
            TcpTableBasicAll = 2,
            TcpTableOwnerPidListener = 3,
            TcpTableOwnerPidConnections = 4,
            TcpTableOwnerPidAll = 5,
            TcpTableOwnerModuleListener = 6,
            TcpTableOwnerModuleConnections = 7,
            TcpTableOwnerModuleAll = 8
        }

        [DllImport(Libraries.IpHlpApi)]
        internal static extern uint GetTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool order);

        [DllImport(Libraries.IpHlpApi)]
        internal static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref uint dwOutBufLen,
            bool order,
            uint IPVersion,
            TcpTableClass tableClass,
            uint reserved);
    }
}
#endif