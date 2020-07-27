using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    public class HostMetricsCollector
    {
        private readonly Action<HostMetrics> nativeCollector;

        public HostMetricsCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                nativeCollector = new NativeHostMetricsCollector_Windows().Collect;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                nativeCollector = new NativeHostMetricsCollector_Linux().Collect;
        }

        [NotNull]
        public HostMetrics Collect()
        {
            var metrics = new HostMetrics();
            CollectNativeMetrics(metrics);
            throw new NotImplementedException();
            return metrics;
        }

        private void CollectNativeMetrics(HostMetrics metrics)
            => nativeCollector?.Invoke(metrics);
    }
}