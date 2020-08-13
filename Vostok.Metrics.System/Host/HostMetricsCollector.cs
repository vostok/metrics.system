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
    public class HostMetricsCollector : IDisposable
    {
        private readonly Action<HostMetrics> nativeCollector;
        private readonly Action disposeNativeCollector;
        private readonly DiskSpaceCollector diskSpaceCollector = new DiskSpaceCollector();
        private readonly TcpStateCollector tcpStateCollector = new TcpStateCollector();

        public void Dispose()
        {
            disposeNativeCollector?.Invoke();
            diskSpaceCollector?.Dispose();
        }

        public HostMetricsCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var collector = new NativeHostMetricsCollector_Windows();
                nativeCollector = collector.Collect;
                disposeNativeCollector = collector.Dispose;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var collector = new NativeHostMetricsCollector_Linux();
                nativeCollector = collector.Collect;
                disposeNativeCollector = collector.Dispose;
            }
        }

        [NotNull]
        public HostMetrics Collect()
        {
            var metrics = new HostMetrics();
            CollectNativeMetrics(metrics);
            diskSpaceCollector.Collect(metrics);
            tcpStateCollector.Collect(metrics);
            return metrics;
        }

        private void CollectNativeMetrics(HostMetrics metrics)
            => nativeCollector?.Invoke(metrics);
    }
}