﻿using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class HostMetrics
    {
        /// <summary>
        /// Total number of CPU cores on host.
        /// </summary>
        public int CpuTotalCores { get; set; }

        /// <summary>
        /// <para>Number of CPU cores utilized by host.</para>
        /// <para>This metric has a value between 0 and <see cref="CpuTotalCores"/>.</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double CpuUtilizedCores { get; set; }

        /// <summary>
        /// <para>Fraction of the CPU resources consumed by host (relative to all cores).</para>
        /// <para>This metric has a value between 0 and 1.</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double CpuUtilizedFraction { get; set; }
        
        /// <summary>
        /// <para>Fraction of the CPU resources consumed by host in kernel mode</para>
        /// <para>This metric has a value between 0 and 1.</para>
        /// </summary>
        public double CpuUtilizedFractionInKernel { get; set; }

        /// <summary>
        /// Amount of physical RAM on host.
        /// </summary>
        public long MemoryTotal { get; set; }

        /// <summary>
        /// Amount of physical RAM available for starting new applications.
        /// </summary>
        public long MemoryAvailable { get; set; }

        /// <summary>
        /// Amount of free physical RAM.
        /// </summary>
        public long MemoryFree { get; set; }

        /// <summary>
        /// Amount of physical RAM consumed by system kernel.
        /// </summary>
        public long MemoryKernel { get; set; }

        /// <summary>
        /// Amount of physical RAM used as cache memory.
        /// </summary>
        public long MemoryCached { get; set; }

        /// <summary>
        /// Amount of hard page faults per second.
        /// </summary>
        public long PageFaultsPerSecond { get; set; }

        /// <summary>
        /// The current number of processes on host.
        /// </summary>
        public int ProcessCount { get; set; }

        /// <summary>
        /// The current number of threads on host.
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// The current number of open handles on host.
        /// </summary>
        public int HandleCount { get; set; }

        /// <summary>
        /// Disk space info per volume.
        /// </summary>
        public Dictionary<string, DiskSpaceInfo> DisksSpaceInfo { get; set; }

        /// <summary>
        /// Disk usage info per device.
        /// null if failed to obtain info or disabled
        /// </summary>
        [CanBeNull]
        public Dictionary<string, DiskUsageInfo> DisksUsageInfo { get; set; }

        /// <summary>
        /// TCP connections per state.
        /// </summary>
        public Dictionary<TcpState, int> TcpStates { get; set; }

        /// <summary>
        /// Network usage info per interface.
        /// </summary>
        public Dictionary<string, NetworkInterfaceUsageInfo> NetworkInterfacesUsageInfo { get; set; }

        /// <summary>
        /// Amount of network sent bytes per second (across all interfaces).
        /// </summary>
        public long NetworkSentBytesPerSecond { get; set; }

        /// <summary>
        /// Amount of network received bytes per second (across all interfaces).
        /// </summary>
        public long NetworkReceivedBytesPerSecond { get; set; }

        /// <summary>
        /// Maximum host network speed (across all interfaces).
        /// </summary>
        public long NetworkBandwidthBytesPerSecond { get; set; }

        /// <summary>
        /// <para>Utilized percent of the output network bandwidth (relative to all interfaces).</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double NetworkOutUtilizedPercent
        {
            get
            {
                if (NetworkBandwidthBytesPerSecond == 0)
                    return 0;
                return (NetworkSentBytesPerSecond * 100d / NetworkBandwidthBytesPerSecond).Clamp(0, 100);
            }
        }

        /// <summary>
        /// <para>Utilized percent of the input network bandwidth (relative to all interfaces).</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double NetworkInUtilizedPercent
        {
            get
            {
                if (NetworkBandwidthBytesPerSecond == 0)
                    return 0;
                return (NetworkReceivedBytesPerSecond * 100d / NetworkBandwidthBytesPerSecond).Clamp(0, 100);
            }
        }

        /// <summary>
        /// Amount of total TCP connections count.
        /// </summary>
        public int TcpConnectionsTotalCount => TcpStates?.Sum(x => x.Value) ?? 0;
    }
}