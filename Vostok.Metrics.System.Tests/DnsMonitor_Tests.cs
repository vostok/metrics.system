#if NET5_0 || NET6_0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Metrics.System.Dns;
using Vostok.Metrics.System.Tests.Helpers;
using DNS = System.Net.Dns;

namespace Vostok.Metrics.System.Tests
{
    [TestFixture]
    internal class DnsMonitor_Tests : IObserver<DnsLookupInfo>
    {
        private List<DnsLookupInfo> collections;
        private DnsMonitor monitor;

        [SetUp]
        public void TestSetup()
        {
            collections = new List<DnsLookupInfo>();
            monitor?.Dispose();
            monitor = new DnsMonitor();
            monitor.Subscribe(this);
        }

        [Test]
        public void T()
        {
            var dictionary = new Dictionary<IPAddress, int>();
            var globalProps = IPGlobalProperties.GetIPGlobalProperties();
            foreach (var tcpConnection in globalProps.GetActiveTcpConnections())
            {
                var ep = tcpConnection.LocalEndPoint;
                var address = ep.Address;
                
                dictionary.TryAdd(address, 0);
                dictionary[address]++;
            }
            
            foreach (var tcpConnection in globalProps.GetActiveTcpListeners())
            {
                var address = tcpConnection.Address;
                
                dictionary.TryAdd(address, 0);
                dictionary[address]++;
            }

            foreach (var kvp in dictionary)
            {
                Console.WriteLine($"Addres: {kvp.Key}, Count: {kvp.Value}");
            }
        }

        [Test]
        public void Should_measure_failed_lookups()
        {
            const int lookupsCount = 5;
            for (var i = 0; i < lookupsCount; i++)
                try
                {
                    DNS.GetHostEntry("go23t2dst2ogle.c23t4vgwom");
                }
                catch
                {
                    // Ignore
                }

            Action assertion = () => collections.Should().HaveCount(lookupsCount);

            assertion.ShouldPassIn(30.Seconds());
            collections.All(info => info.IsFailed).Should().BeTrue();
        }

        [Test]
        public void Should_measure_lookups()
        {
            const int lookupsCount = 5;
            for (var i = 0; i < lookupsCount; i++)
                DNS.GetHostEntry("google.com");

            Action assertion = () => collections.Should().HaveCount(lookupsCount);

            assertion.ShouldPassIn(5.Seconds());
            collections.Any(info => info.IsFailed).Should().BeFalse();
        }

        [Test]
        public void Should_measure_lookup_latency()
        {
            var stopWatch = Stopwatch.StartNew();
            DNS.GetHostEntry("google.com");
            stopWatch.Stop();

            Action assertion = () => collections.Should().HaveCount(1);

            assertion.ShouldPassIn(5.Seconds());
            var latency = collections.Single().Latency;

            Math.Abs(stopWatch.ElapsedMilliseconds - latency.TotalMilliseconds).Should().BeLessThan(1);
        }

        public void OnNext(DnsLookupInfo value)
            => collections.Add(value);

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
#endif