using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Host;

namespace Vostok.Metrics.System.Tests
{
    [TestFixture]
    public class HostMetricsCollector_Tests
    {
        private HostMetricsCollector collector;

        [SetUp]
        public void TestSetup()
        {
            collector = new HostMetricsCollector();
            collector.Collect();
        }

        [Test]
        public void Should_measure_cpu_utilization()
        {
            var watch = Stopwatch.StartNew();

            while (watch.Elapsed.TotalMilliseconds < 200)
                new SpinWait().SpinOnce();

            var metrics = collector.Collect();

            metrics.CpuUtilizedCores.Should().BeGreaterThan(0);
            metrics.CpuUtilizedFraction.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_memory_consumption()
        {
            var metrics = collector.Collect();

            metrics.MemoryTotal.Should().BeGreaterThan(0);
            metrics.MemoryKernel.Should().BeGreaterThan(0);
            metrics.MemoryCached.Should().BeGreaterThan(0);
            metrics.MemoryAvailable.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_disk_space()
        {
            var metrics = collector.Collect();

            metrics.DiskSpaceInfos.Should().NotBeEmpty();

            foreach (var diskSpaceInfo in metrics.DiskSpaceInfos)
            {
                diskSpaceInfo.Name.Should().NotBeNullOrEmpty();
                diskSpaceInfo.FreeBytes.Should().BeGreaterThan(0);
                diskSpaceInfo.FreePercent.Should().BeGreaterThan(0);
                diskSpaceInfo.TotalCapacity.Should().BeGreaterThan(0);
            }
        }
    }
}