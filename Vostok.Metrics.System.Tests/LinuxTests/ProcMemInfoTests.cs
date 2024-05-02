using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers.Linux;

namespace Vostok.Metrics.System.Tests.LinuxTests;

public class ProcMemInfoTests
{
    [Test]
    public void Test_CorrectReading()
    {
        using var reader = CreateReaderWithContent();

        reader.TryRead(out var v).Should().Be(true);

        v.TotalMemory.Should().Be(65749580 * 1024L);
        v.FreeMemory.Should().Be(26587656 * 1024L);
        v.AvailableMemory.Should().Be(49311872 * 1024L);
        v.CacheMemory.Should().Be(15522376 * 1024L);
        v.KernelMemory.Should().Be(3547500 * 1024L);
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

    private static ProcMemInfoReader CreateReaderWithContent() =>
        new ProcMemInfoReader(new MemoryStream(File.ReadAllBytes("Resources/proc_meminfo.bin")));
}