using BenchmarkDotNet.Running;

namespace Vostok.Metrics.System.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<MetricsCollectorBenchmark>();
        }
    }
}