using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Host;

namespace Vostok.Metrics.System.Tests;

[TestFixture]
internal class HostMonitor_Tests
{
    [Test]
    public void CurrentProcessMonitor_ShouldNotThrow_OnMultipleDisposeCalls()
    {
        var monitor = new HostMonitor();
        monitor.Dispose();
        var dispose = () => monitor.Dispose();
        dispose.Should().NotThrow();
    }
}