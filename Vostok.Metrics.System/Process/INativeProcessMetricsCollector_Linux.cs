namespace Vostok.Metrics.System.Process;

// ReSharper disable once InconsistentNaming
internal interface INativeProcessMetricsCollector_Linux
{
    void Collect(CurrentProcessMetrics metrics);
    void Dispose();
}