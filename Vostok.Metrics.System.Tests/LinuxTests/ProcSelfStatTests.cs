using System;
using System.IO;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers.Linux;

namespace Vostok.Metrics.System.Tests.LinuxTests;

public class ProcSelfStatTests
{
#if NET6_0_OR_GREATER
    private readonly string content = "2965399 ((z ) c) S 902426 902426 0 -1 1077936384 9221967 0 175 0 17631617 4500158 34670 0 20 0 81 0 59485697 28498935808 184603 18446744073709551615 1 1 0 0 0 0 0 4096 148734 0 0 0 17 10 0 0 4 0 0 0 0 0 0 0 0 0 0";
#else
    private readonly string content = "2965399 (no_spaces) S 902426 902426 0 -1 1077936384 9221967 0 175 0 17631617 4500158 34670 0 20 0 81 0 59485697 28498935808 184603 18446744073709551615 1 1 0 0 0 0 0 4096 148734 0 0 0 17 10 0 0 4 0 0 0 0 0 0 0 0 0 0";
#endif

    [Test]
    public void Test_CorrectReading()
    {
        using var reader = new ProcSelfStatReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        reader.TryRead(out var s).Should().Be(true);

        s.utime.Should().Be(4500158);
        s.stime.Should().Be(34670);
    }

    [Test]
    public void Test_FastBench()
    {
        using var reader = new ProcSelfStatReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));

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
}