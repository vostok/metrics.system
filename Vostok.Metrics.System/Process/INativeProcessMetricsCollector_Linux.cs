using System;

namespace Vostok.Metrics.System.Process;

// ReSharper disable once InconsistentNaming
internal interface INativeProcessMetricsCollector_Linux : IDisposable
{
    void Collect(CurrentProcessMetrics metrics);
}