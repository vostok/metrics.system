using JetBrains.Annotations;

namespace Vostok.Metrics.System.Process
{
    // TODO(iloktionov): xml docs for all properties

    [PublicAPI]
    public class CurrentProcessMetrics
    {
        public double CpuUtilizedCores { get; set; }

        public double CpuUtilizedFraction { get; set; }

        public long MemoryResident { get; set; }

        public long MemoryPrivate { get; set; }
        
        public long GcHeapSize { get; set; }

        public long GcGen0Size { get; set; }

        public long GcGen1Size { get; set; }

        public long GcGen2Size { get; set; }

        public long GcAllocatedBytes { get; set; }

        public long GcLOHSize { get; set; }

        public int GcGen0Collections { get; set; }

        public int GcGen1Collections { get; set; }
        
        public int GcGen2Collections { get; set; }

        public double GcTimePercent { get; set; }

        public int ThreadPoolTotalCount { get; set; }

        public int ThreadPoolMinWorkers { get; set; }

        public int ThreadPoolMinIocp { get; set; }

        public int ThreadPoolBusyWorkers { get; set; }

        public int ThreadPoolBusyIocp { get; set; }

        public long ThreadPoolQueueLength { get; set; }

        public int HandlesCount { get; set; }

        public int ExceptionsCount { get; set; }

        public int LockContentionCount { get; set; }
        
        public long ActiveTimersCount { get; set; }
    }
}
