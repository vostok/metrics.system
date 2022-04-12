using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Process;

namespace Vostok.Metrics.System.Tests;

[TestFixture]
internal class CurrentProcessMonitor_Tests
{
    [Test]
    public void CurrentProcessMonitor_ShouldNotThrow_OnMultipleDisposeCalls()
    {
        var monitor = new CurrentProcessMonitor();
        monitor.Dispose();
        var dispose = () => monitor.Dispose();
        dispose.Should().NotThrow();
    }
}