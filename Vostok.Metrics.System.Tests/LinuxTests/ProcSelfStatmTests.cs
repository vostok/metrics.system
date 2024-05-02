using System;
using System.IO;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Metrics.System.Helpers.Linux;

namespace Vostok.Metrics.System.Tests.LinuxTests;

public class ProcSelfStatmTests
{
    private readonly string content = "1405 146 129 7 0 111 0";

    [Test]
    public void Test_CorrectReading()
    {
        using var reader = new ProcSelfStatmReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        reader.pageSize = 4096;

        //user nuce system idle
        reader.TryRead(out var s).Should().Be(true);

        s.PrivateRss = (146 - 129) * 4096;
        s.DataSize = 111 * 4096;
    }

    [Test]
    public void Test_FastBench()
    {
        using var reader = new ProcSelfStatmReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        reader.pageSize = 4096;

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