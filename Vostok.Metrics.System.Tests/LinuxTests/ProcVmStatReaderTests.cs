using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers.Linux;

namespace Vostok.Metrics.System.Tests.LinuxTests;

public class ProcVmStatReaderTests
{
    [Test]
    public void Test_CorrectReading()
    {
        using var reader = CreateReaderWithContent();

        reader.TryRead(out var v).Should().Be(true);

        v.pgmajfault.Should().Be(6742006);
    }

    [Test]
    public void Test_FastBench()
    {
        using var reader = CreateReaderWithContent();

        reader.TryRead(out var s);

        var duration = TimeSpan.FromSeconds(1);
        var count = 0;
        var meter = new StatsMeter();
        while (meter.Elapsed < duration)
        {
            reader.TryRead(out s);
            count++;
        }

        meter.Print(count);
    }

    private static ProcVmStatReader CreateReaderWithContent() =>
        new ProcVmStatReader(new MemoryStream(File.ReadAllBytes("Resources/proc_vmstat.bin")));
}