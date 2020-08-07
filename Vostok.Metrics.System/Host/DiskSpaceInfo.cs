using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskSpaceInfo
    {
        public string DiskName { get; set; }
        public long TotalCapacityBytes { get; set; }
        public long FreeBytes { get; set; }
        public double FreePercent { get; set; }
    }
}