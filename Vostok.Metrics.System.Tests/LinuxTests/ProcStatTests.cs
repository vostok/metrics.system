using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers.Linux;

namespace Vostok.Metrics.System.Tests.LinuxTests;

public class ProcStatTests
{
    [Test]
    public void Test_CorrectReading()
    {
        using var reader = CreateReaderWithContent();

        //user nuce system idle
        reader.TryRead(out var s).Should().Be(true);
        s.UserTime.Should().Be(185964739);

        s.NicedTime.Should().Be(14263);

        s.SystemTime.Should().Be(24951354);
        s.IdleTime.Should().Be(2007236828);

        //s.CpuCount.Should().Be(12);
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

    private static ProcStatReader CreateReaderWithContent() =>
        new ProcStatReader(false, new MemoryStream(File.ReadAllBytes("Resources\\proc_stat.bin")));
}