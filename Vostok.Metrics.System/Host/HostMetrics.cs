using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class HostMetrics
    {
        /// <summary>
        /// <para>Number of CPU cores utilized by host.</para>
        /// <para>This metric has a value between 0 and <see cref="Environment.ProcessorCount"/>.</para>
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
        /// Amount of physical RAM on host.
        /// </summary>
        public long MemoryTotal { get; set; }

        /// <summary>
        /// Amount of physical RAM available for starting new applications.
        /// </summary>
        public long MemoryAvailable { get; set; }

        /// <summary>
        /// Amount of physical RAM consumed by system kernel.
        /// </summary>
        public long MemoryKernel { get; set; }

        /// <summary>
        /// Amount of physical RAM used as cache memory.
        /// </summary>
        public long MemoryCached { get; set; }

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
        public Dictionary<string, DiskSpaceInfo> DiskSpaceInfos { get; set; }

        /// <summary>
        /// TCP connections per state.
        /// </summary>
        public Dictionary<TcpState, int> TcpStates { get; set; }

        /// <summary>
        /// Network sent bytes per second (across all interfaces).
        /// </summary>
        public double NetworkSentBytesPerSecond { get; set; }

        /// <summary>
        /// Network received bytes per second (across all interfaces).
        /// </summary>
        public double NetworkReceivedBytesPerSecond { get; set; }

        /// <summary>
        /// Total TCP connections count.
        /// </summary>
        public int TcpConnectionsTotalCount => TcpStates.Sum(x => x.Value);
    }
}