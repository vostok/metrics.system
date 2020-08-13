using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskUsageInfo
    {
        public string DiskName { get; set; }
        public double UtilizedPercent { get; set; }
        public TimeSpan ReadAverageLatency { get; set; }
        public TimeSpan WriteAverageLatency { get; set; }
        public double ReadsPerSecond { get; set; }
        public double WritesPerSecond { get; set; }
        public long CurrentQueueLength { get; set; }
    }
}