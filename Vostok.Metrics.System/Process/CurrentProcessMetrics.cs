using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public class CurrentProcessMetrics
    {
        /// <summary>
        /// <para>Number of CPU cores utilized by current process.</para>
        /// <para>This metric has a value between 0 and <see cref="Environment.ProcessorCount"/>.</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double CpuUtilizedCores { get; set; }

        /// <summary>
        /// <para>Number of CPU cores allowed to use by current process.</para>
        /// <para>Defaults to <see cref="Environment.ProcessorCount"/> if there's no externally imposed limit.</para>
        /// </summary>
        public double CpuLimitCores { get; set; }

        /// <summary>
        /// <para>Fraction of the CPU resources consumed by current process (relative to <see cref="CpuLimitCores"/>).</para>
        /// <para>This metric has a value between 0 and 1.</para>
        /// <para>This metric is an average value between two observation moments (current and previous).</para>
        /// </summary>
        public double CpuUtilizedFraction { get; set; }

        /// <summary>
        /// <para>Amount of physical RAM consumed by current process in bytes.</para>
        /// <para>This maps into WorkingSet on Windows and ResidentSize on Linux.</para>
        /// </summary>
        public long MemoryResident { get; set; }

        /// <summary>
        /// <para>Amount of private memory allocated for current process in bytes.</para>
        /// <para>This maps into PrivateBytes on Windows and VmData on Linux.</para>
        /// </summary>
        public long MemoryPrivate { get; set; }

        /// <summary>
        /// <para>Amount of physical memory allowed to use by current process.</para>
        /// <para>Defaults to total physical memory on host if there's no externally imposed limit.</para>
        /// </summary>
        public long MemoryLimit { get; set; }

        /// <summary>
        /// <para>Fraction of the memory resources consumed by current process (<see cref="MemoryResident"/> relative to <see cref="MemoryLimit"/>).</para>
        /// <para>This metric has a value between 0 and 1.</para>
        /// </summary>
        public double MemoryUtilizedFraction { get; set; }

        /// <summary>
        /// Total size of all GC heaps in bytes.
        /// </summary>
        public long GcHeapSize { get; set; }

        /// <summary>
        /// Total size of gen-0 managed objects in bytes. Only updated during garbage collections.
        /// </summary>
        public long GcGen0Size { get; set; }

        /// <summary>
        /// Total size of gen-1 managed objects in bytes. Only updated during garbage collections.
        /// </summary>
        public long GcGen1Size { get; set; }

        /// <summary>
        /// Total size of gen-2 managed objects in bytes. Only updated during garbage collections.
        /// </summary>
        public long GcGen2Size { get; set; }

        /// <summary>
        /// Total size of LOH managed objects in bytes. Only updated during garbage collections.
        /// </summary>
        public long GcLOHSize { get; set; }

        /// <summary>
        /// <para>Total amount of memory allocated for managed objects since last observation in bytes. Includes recycled garbage.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public long GcAllocatedBytes { get; set; }

        /// <summary>
        /// <para>Number of gen-0 collections since last observation.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public int GcGen0Collections { get; set; }

        /// <summary>
        /// <para>Number of gen-1 collections since last observation.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public int GcGen1Collections { get; set; }

        /// <summary>
        /// <para>Number of gen-2 collections since last observation.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public int GcGen2Collections { get; set; }

        /// <summary>
        /// Percent (0-100) of time spent in garbage collections. Only updated during garbage collections.
        /// </summary>
        public double GcTimePercent { get; set; }

        /// <summary>
        /// Total number of threads in ThreadPool.
        /// </summary>
        public int ThreadPoolTotalCount { get; set; }

        /// <summary>
        /// Soft limit on worker thread count.
        /// </summary>
        public int ThreadPoolMinWorkers { get; set; }

        /// <summary>
        /// Soft limit on IO thread count.
        /// </summary>
        public int ThreadPoolMinIo { get; set; }

        /// <summary>
        /// Number of active threads currently executing work items.
        /// </summary>
        public int ThreadPoolBusyWorkers { get; set; }

        /// <summary>
        /// Number of active threads currently executing IO callbacks.
        /// </summary>
        public int ThreadPoolBusyIo { get; set; }

        /// <summary>
        /// Number of work items waiting for a thread in the queue.
        /// </summary>
        public long ThreadPoolQueueLength { get; set; }

        /// <summary>
        /// Number of file handles opened by current process.
        /// </summary>
        public int HandlesCount { get; set; }

        /// <summary>
        /// <para>Number of managed exceptions thrown since last observation.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public int ExceptionsCount { get; set; }

        /// <summary>
        /// <para>Number of lock contention events since last observation.</para>
        /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
        /// </summary>
        public int LockContentionCount { get; set; }
        
        /// <summary>
        /// Number of active timers in a process. An active timer is scheduled to fire sometime in the future.
        /// </summary>
        public long ActiveTimersCount { get; set; }
    }
}
