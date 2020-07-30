﻿using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    /// <summary>
    /// <para><see cref="HostMetricsCollector"/> collects and returns <see cref="HostMetrics"/>.</para>
    /// <para>It is designed to be invoked periodically.</para>
    /// </summary>
    [PublicAPI]
    public class HostMetricsCollector
    {
        private readonly Action<HostMetrics> nativeCollector;
        private readonly DiskSpaceCollector diskSpaceCollector = new DiskSpaceCollector();
        private readonly NetworkInfoCollector networkInfoCollector = new NetworkInfoCollector();

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
            diskSpaceCollector.Collect(metrics);
            networkInfoCollector.Collect(metrics);
            return metrics;
        }

        private void CollectNativeMetrics(HostMetrics metrics)
            => nativeCollector?.Invoke(metrics);
    }
}