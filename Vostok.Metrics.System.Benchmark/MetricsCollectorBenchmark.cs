using BenchmarkDotNet.Attributes;
using Vostok.Metrics.System.Host;
using Vostok.Metrics.System.Process;

namespace Vostok.Metrics.System.Benchmark
{
    [MemoryDiagnoser]
    public class MetricsCollectorBenchmark
    {
        private readonly CurrentProcessMetricsCollector processCollector = new CurrentProcessMetricsCollector();
        private readonly HostMetricsCollector hostCollector = new HostMetricsCollector();

        [Benchmark]
        public void CollectProcessMetrics() => processCollector.Collect();

        [Benchmark]
        public void CollectHostMetrics() => new HostMetricsCollector().Collect();
    }
}