using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskUsageInfo
    {
        public string DiskName { get; set; }
        public double IdleTimePercent { get; set; }
        public double ReadLatency { get; set; }
        public double WriteLatency { get; set; }
        public double ReadsPerSecond { get; set; }
        public double WritesPerSecond { get; set; }
        public long CurrentQueueLength { get; set; }
    }
}