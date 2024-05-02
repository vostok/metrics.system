using System;

namespace Vostok.Metrics.System.Host;

// ReSharper disable once InconsistentNaming
internal interface INativeHostMetricsCollector_Linux : IDisposable
{
    void Collect(HostMetrics metrics);
}