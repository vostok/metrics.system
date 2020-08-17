using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskUsageInfo
    {
        public string DiskName { get; set; }
        public double UtilizedPercent { get; set; }
        public long ReadAverageMsLatency { get; set; }
        public long WriteAverageMsLatency { get; set; }
        public long ReadsPerSecond { get; set; }
        public long WritesPerSecond { get; set; }
        public long BytesReadPerSecond { get; set; }
        public long BytesWrittenPerSecond { get; set; }
        public long CurrentQueueLength { get; set; }
    }
}