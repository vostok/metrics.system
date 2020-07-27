using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class HostMetrics
    {
        /// <summary>
        /// <para>Number of CPU cores utilized by current process.</para>
        /// <para>This metric has a value between 0 and <see cref="Environment.ProcessorCount"/>.</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double CpuUtilizedCores { get; set; }

        /// <summary>
        /// <para>Fraction of the CPU resources consumed by current process (relative to all cores).</para>
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
    }
}