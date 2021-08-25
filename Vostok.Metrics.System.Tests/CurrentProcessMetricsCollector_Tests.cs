using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Metrics.System.Process;

namespace Vostok.Metrics.System.Tests
{
    [TestFixture]
    internal class CurrentProcessMetricsCollector_Tests
    {
        private CurrentProcessMetricsCollector collector;

        [SetUp]
        public void TestSetup()
        {
            collector?.Dispose();
            collector = new CurrentProcessMetricsCollector();
            collector.Collect();
        }

        [Test]
        public void Should_measure_active_timers_count()
        {
            using (new Timer(_ => {}, null, TimeSpan.Zero, 5.Seconds()))
                collector.Collect().ActiveTimersCount.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Should_measure_lock_contention_count()
        {
            var sync = new object();

            var task1 = Task.Run(() =>
            {
                lock (sync)
                    Thread.Sleep(2000);
            });

            var task2 = Task.Run(() =>
            {
                lock (sync)
                    Thread.Sleep(2000);
            });

            await Task.WhenAll(task1, task2);

            collector.Collect().LockContentionCount.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_exceptions_count()
        {
            for (var i = 1; i < 5; i++)
            {
                for (var j = 0; j < i; j++)
                    try
                    {
                        throw new Exception();
                    }
                    catch
                    {
                        // ignore
                    }

                collector.Collect().ExceptionsCount.Should().BeGreaterOrEqualTo(i);
            }
        }

        [Test]
        public void Should_measure_handles_count()
            => collector.Collect().HandlesCount.Should().BeGreaterThan(0);

        [Test]
        public void Should_measure_cpu_utilization()
        {
            var watch = Stopwatch.StartNew();

            while (watch.Elapsed.TotalMilliseconds < 2000)
                new SpinWait().SpinOnce();

            var metrics = collector.Collect();

            metrics.CpuUtilizedCores.Should().BeGreaterThan(0);
            metrics.CpuUtilizedFraction.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_memory_consumption()
        {
            var metrics = collector.Collect();

            metrics.MemoryPrivate.Should().BeGreaterThan(0);
            metrics.MemoryResident.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_gc_metrics()
        {
            GC.Collect(0, GCCollectionMode.Forced);
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);

            var metrics = collector.Collect();

            metrics.GcGen0Collections.Should().BeGreaterThan(0);
            metrics.GcGen1Collections.Should().BeGreaterThan(0);
            metrics.GcGen2Collections.Should().BeGreaterThan(0);
            metrics.GcHeapSize.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_measure_outgoing_socket_connections()
        {
            var client = new HttpClient();

            client.GetAsync("https://www.google.com/").GetAwaiter().GetResult();

            var metrics = collector.Collect();

            metrics.OutgoingConnectionsCount.Should().Be(1);
            metrics.IncomingConnectionsCount.Should().Be(0);
            metrics.FailedConnectionsCount.Should().Be(0);
        }

        [Test]
        public void Should_measure_incoming_socket_connections()
        {
            const int connectionCount = 10;
            var ipEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 11000);

            var sListener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            sListener.Bind(ipEndPoint);
            sListener.Listen(10);

            Parallel.For(0,
                connectionCount,
                i =>
                {
                    var sSender = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    sSender.Connect(ipEndPoint);
                });

            for (var i = 0; i < connectionCount; i++)
            {
                sListener.Accept();
            }

            var metrics = collector.Collect();

            metrics.OutgoingConnectionsCount.Should().Be(connectionCount);
            metrics.IncomingConnectionsCount.Should().Be(connectionCount);
            metrics.FailedConnectionsCount.Should().Be(0);
        }

        [Test]
        public void Should_measure_failed_socket_connections()
        {
            const int connectionCount = 10;
            var wrongEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 11000);

            Parallel.For(0,
                connectionCount,
                i =>
                {
                    var sSender = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        sSender.Connect(wrongEndPoint);
                    }
                    catch
                    {
                        // Ignore
                    }
                });

            var metrics = collector.Collect();

            metrics.FailedConnectionsCount.Should().Be(connectionCount);
            metrics.OutgoingConnectionsCount.Should().Be(connectionCount);
            metrics.IncomingConnectionsCount.Should().Be(0);
        }
    }
}