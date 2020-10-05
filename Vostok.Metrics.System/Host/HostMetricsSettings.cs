using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class HostMetricsSettings
    {
        public static HostMetricsSettings CreateDisabled()
            => new HostMetricsSettings
            {
                CollectCpuMetrics = false,
                CollectMiscMetrics = false,
                CollectMemoryMetrics = false,
                CollectDiskUsageMetrics = false,
                CollectDiskSpaceMetrics = false,
                CollectNetworkUsageMetrics = false,
                CollectTcpStateMetrics = false
            };

        public bool CollectCpuMetrics { get; set; } = true;

        public bool CollectMemoryMetrics { get; set; } = true;

        public bool CollectDiskSpaceMetrics { get; set; } = true;

        public bool CollectDiskUsageMetrics { get; set; } = true;

        public bool CollectNetworkUsageMetrics { get; set; } = true;

        public bool CollectTcpStateMetrics { get; set; } = true;

        public bool CollectMiscMetrics { get; set; } = true;
    }
}
