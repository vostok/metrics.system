using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskSpaceInfo
    {
        public string Name { get; internal set; }
        public long TotalCapacityBytes { get; internal set; }
        public long FreeBytes { get; internal set; }
        public double FreePercent { get; internal set; }
    }
}