﻿using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Host.Legacy;

namespace Vostok.Metrics.System.Host
{
    /// <summary>
    /// <para><see cref="HostMetricsCollector"/> collects and returns <see cref="HostMetrics"/>.</para>
    /// <para>It is designed to be invoked periodically.</para>
    /// </summary>
    [PublicAPI]
    public class HostMetricsCollector : IDisposable
    {
        private static readonly TimeSpan CacheTTL = TimeSpan.FromMilliseconds(100);

        private readonly HostMetricsSettings settings;
        private readonly Action<HostMetrics> nativeCollector;
        private readonly Action disposeNativeCollector;
        private readonly DiskSpaceCollector diskSpaceCollector = new DiskSpaceCollector();
        private readonly TcpStateCollector tcpStateCollector = new TcpStateCollector();
        private readonly ThrottlingCache<HostMetrics> cache;

        public HostMetricsCollector()
            : this(new HostMetricsSettings())
        {
        }

        public HostMetricsCollector(HostMetricsSettings settings)
        {
            this.settings = settings ?? new HostMetricsSettings();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var collector = new NativeHostMetricsCollector_Windows(this.settings);
                nativeCollector = collector.Collect;
                disposeNativeCollector = collector.Dispose;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var legacyCollectorFeatureFlag = Environment
                    .GetEnvironmentVariable(VostokSystemMetricsConstants.UseLegacyMetricsCollectorEnvironmentVariable);
                var useLegacyCollector = legacyCollectorFeatureFlag?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;
                INativeHostMetricsCollector_Linux collector = useLegacyCollector
#pragma warning disable CS0612
                    ? new LegacyNativeHostMetricsCollector_Linux(this.settings)
#pragma warning restore CS0612
                    : new NativeHostMetricsCollector_Linux(this.settings);
                InternalLogger.Debug(nameof(HostMetricsCollector), $"Legacy metrics collector feature flag: '{legacyCollectorFeatureFlag}', using {collector.GetType()}");
                nativeCollector = collector.Collect;
                disposeNativeCollector = collector.Dispose;
            }

            cache = new ThrottlingCache<HostMetrics>(CollectInternal, CacheTTL);
        }

        public void Dispose()
        {
            disposeNativeCollector?.Invoke();
            diskSpaceCollector?.Dispose();
        }

        [NotNull]
        public HostMetrics Collect()
            => cache.Obtain();

        private HostMetrics CollectInternal()
        {
            var metrics = new HostMetrics();

            CollectNativeMetrics(metrics);

            if (settings.CollectDiskSpaceMetrics)
                diskSpaceCollector.Collect(metrics);

            if (settings.CollectTcpStateMetrics)
                tcpStateCollector.Collect(metrics);

            return metrics;
        }

        private void CollectNativeMetrics(HostMetrics metrics)
            => nativeCollector?.Invoke(metrics);
    }
}