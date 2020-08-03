﻿using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Host;

namespace Vostok.Metrics.System.Tests
{
    [TestFixture]
    internal class HostMetricsCollector_Tests
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
                diskSpaceInfo.Value.Name.Should().NotBeNullOrEmpty();
                diskSpaceInfo.Value.FreeBytes.Should().BeGreaterThan(0);
                diskSpaceInfo.Value.FreePercent.Should().BeGreaterThan(0);
                diskSpaceInfo.Value.TotalCapacityBytes.Should().BeGreaterThan(0);
            }
        }

        [Test]
        public void Should_measure_performance_info()
        {
            var metrics = collector.Collect();

            metrics.HandleCount.Should().BeGreaterThan(0);
            metrics.ThreadCount.Should().BeGreaterThan(0);
            metrics.ProcessCount.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_tcp_states()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                throw new InconclusiveException("No network available");
            var metrics = collector.Collect();

            metrics.TcpStateMetrics.Should().NotBeEmpty();
            metrics.TcpConnectionsTotalCount.Should().BeGreaterThan(0);
        }
    }
}