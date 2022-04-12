using System;
using System.Collections;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Metrics.Senders;
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

            while (watch.Elapsed.TotalMilliseconds < 2000)
                new SpinWait().SpinOnce();

            var metrics = collector.Collect();

            metrics.CpuUtilizedCores.Should().BeGreaterThan(0);
            metrics.CpuUtilizedFraction.Should().BeGreaterThan(0).And.BeLessOrEqualTo(100);
        }

        [Test]
        public void Should_measure_memory_consumption()
        {
            var metrics = collector.Collect();

            metrics.MemoryTotal.Should().BeGreaterThan(0);
            metrics.MemoryKernel.Should().BeGreaterThan(0);
            metrics.MemoryCached.Should().BeGreaterThan(0);
            metrics.MemoryAvailable.Should().BeGreaterThan(0);
            metrics.MemoryFree.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_disk_space()
        {
            var metrics = collector.Collect();

            metrics.DisksSpaceInfo.Should().NotBeEmpty();

            foreach (var diskSpaceInfo in metrics.DisksSpaceInfo)
            {
                diskSpaceInfo.Value.DiskName.Should().NotBeNullOrEmpty();
                diskSpaceInfo.Value.FreeBytes.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(diskSpaceInfo.Value.TotalCapacityBytes);
                diskSpaceInfo.Value.FreePercent.Should().BeGreaterOrEqualTo(0);
                diskSpaceInfo.Value.TotalCapacityBytes.Should().BeGreaterOrEqualTo(0);
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

            metrics.TcpStates.Should().NotBeEmpty();
            metrics.TcpConnectionsTotalCount.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_disk_usage()
        {
            var metrics = collector.Collect();

            metrics.DisksSpaceInfo.Should().NotBeEmpty();

            foreach (var diskUsageInfo in metrics.DisksUsageInfo)
            {
                diskUsageInfo.Value.DiskName.Should().NotBeNullOrEmpty();
                diskUsageInfo.Value.UtilizedPercent.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(100);
            }
        }

        [Test]
        [Explicit("Network should be loaded.")]
        public void Should_measure_network_usage()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                throw new InconclusiveException("No network available");
            var metrics = collector.Collect();

            metrics.NetworkReceivedBytesPerSecond.Should().BeGreaterThan(0);
            metrics.NetworkSentBytesPerSecond.Should().BeGreaterThan(0);
            metrics.NetworkInUtilizedPercent.Should().BeGreaterThan(0).And.BeLessOrEqualTo(100);
            metrics.NetworkOutUtilizedPercent.Should().BeGreaterThan(0).And.BeLessOrEqualTo(100);
        }

        [TestCaseSource(nameof(SettingsSetup))]
        public void Should_report_host_metrics(Action<HostMetricsSettings> settingsSetup)
        {
            var settings = HostMetricsSettings.CreateDisabled();
            var exception = null as Exception;
            var context = new MetricContext(new MetricContextConfig(new DevNullMetricEventSender()) {ErrorCallback = contextException => exception = contextException});

            settingsSetup(settings);
            collector = new HostMetricsCollector(settings);
            collector.ReportMetrics(context, 1.Milliseconds());
            Task.Delay(100).Wait();

            exception.Should().BeNull();
        }

        private static IEnumerable SettingsSetup()
        {
            return new Action<HostMetricsSettings>[]
            {
                settings => settings.CollectCpuMetrics = true,
                settings => settings.CollectMemoryMetrics = true,
                settings => settings.CollectDiskSpaceMetrics = true,
                settings => settings.CollectDiskUsageMetrics = true,
                settings => settings.CollectNetworkUsageMetrics = true,
                settings => settings.CollectTcpStateMetrics = true,
                settings => settings.CollectMiscMetrics = true,
            };
        }
    }
}