#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Logging.Console;
using Vostok.Metrics.System.Gc;

namespace Vostok.Metrics.System.Tests
{
    [TestFixture]
    internal class GarbageCollectionMonitor_Tests : IObserver<GarbageCollectionInfo>
    {
        private List<GarbageCollectionInfo> collections;
        private GarbageCollectionMonitor monitor;

        [SetUp]
        public void TestSetup()
        {
            collections = new List<GarbageCollectionInfo>();
            monitor = new GarbageCollectionMonitor();
            monitor.LogCollections(new SynchronousConsoleLog(), null);
            monitor.Subscribe(this);
        }

        [TearDown]
        public void TestTeardown()
            => monitor.Dispose();

        [Test]
        public void Should_record_induced_garbage_collections()
        {
            GC.Collect(0, GCCollectionMode.Forced);
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);

            Action assertion = () =>
            {
                lock (collections)
                {
                    collections.Should().Contain(gc => gc.Generation == 0 && gc.Reason == GarbageCollectionReason.Induced);
                    collections.Should().Contain(gc => gc.Generation == 1 && gc.Reason == GarbageCollectionReason.Induced);
                    collections.Should().Contain(gc => gc.Generation == 2 && gc.Reason == GarbageCollectionReason.Induced);
                }
            };

            assertion.ShouldPassIn(5.Seconds());
        }

        [Test]
        public void Should_record_collections_with_local_timestamps()
        {
            GC.Collect();

            Action assertion = () =>
            {
                lock (collections)
                {
                    collections.Should().NotBeEmpty();
                    collections.First().StartTimestamp.Should().BeCloseTo(DateTimeOffset.Now, 5.Seconds());
                }
            };

            assertion.ShouldPassIn(5.Seconds());
        }

        public void OnNext(GarbageCollectionInfo value)
        {
            lock (collections)
            {
                collections.Add(value);
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
#endif