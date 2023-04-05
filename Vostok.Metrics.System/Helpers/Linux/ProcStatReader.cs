using System;
using System.IO;

#if NET6_0_OR_GREATER
#else
using System.Linq;
#endif

namespace Vostok.Metrics.System.Helpers.Linux;

/// <summary>
/// zero garbage produced in .net6
/// </summary>
internal readonly struct ProcStatReader : IDisposable
{
    private readonly bool useDotnetCpuCount;

    public ProcStatReader(bool useDotnetCpuCount)
    {
        this.useDotnetCpuCount = useDotnetCpuCount;
#if NET6_0_OR_GREATER
        systemStatReader = new SpanReusableFileReader("/proc/stat");
#else
        systemStatReader = new ReusableFileReader("/proc/stat");
#endif
    }

    public ProcStatReader(bool useDotnetCpuCount, Stream procStatStream)
    {
        this.useDotnetCpuCount = useDotnetCpuCount;
#if NET6_0_OR_GREATER
        systemStatReader = new SpanReusableFileReader(procStatStream);
#else
        systemStatReader = new ReusableFileReader(procStatStream);
#endif
    }

#if NET6_0_OR_GREATER
    private readonly SpanReusableFileReader systemStatReader;
#else
    private readonly ReusableFileReader systemStatReader; //2.7k
#endif

    public bool TryRead(out ProcStat procStat)
    {
        procStat = new ProcStat();
#if NET6_0_OR_GREATER
        systemStatReader.Reset();
        var index = 0;
        //NOTE тут особо длинная строка intr
        while (systemStatReader.TryReadLine(out var line))
        {
            if (index == 0)
            {
                if (!TryParseTimes(line, ref procStat))
                {
                    procStat = default;
                    return false;
                }
            }
            else if (!line.StartsWith("cpu".AsSpan()))
                continue;

            index++;

            if (useDotnetCpuCount)
                break;
        }

        if (useDotnetCpuCount)
            procStat.CpuCount = Environment.ProcessorCount;
        else
            procStat.CpuCount = index - 1;
        return index >= 1;
#else
        return ParseNotOptimized(ref procStat);
#endif
    }

#if NET6_0_OR_GREATER

    private static bool TryParseTimes(ReadOnlySpan<char> line, ref ProcStat ss)
    {
        var e = ProcFsParserHelper.EnumerateTokens(line);

        if (!e.TryMove(0) || !e.Token.SequenceEqual("cpu"))
            return false;

        if (!e.TryMove(1) || !ulong.TryParse(e.Token, out ss.UserTime))
            return false;
        if (!e.TryMove(2) || !ulong.TryParse(e.Token, out ss.NicedTime))
            return false;
        if (!e.TryMove(3) || !ulong.TryParse(e.Token, out ss.SystemTime))
            return false;
        if (!e.TryMove(4) || !ulong.TryParse(e.Token, out ss.IdleTime))
            return false;
        return true;
    }
#else
    private bool ParseNotOptimized(ref ProcStat procStat)
    {
        var correctReads = 0;
        if (FileParser.TrySplitLine(systemStatReader.ReadFirstLine(), 5, out var parts) && parts[0] == "cpu")
        {
            if (ulong.TryParse(parts[1], out var utime))
            {
                correctReads++;
                procStat.UserTime = utime;
            }

            if (ulong.TryParse(parts[2], out procStat.NicedTime))
                correctReads++;

            if (ulong.TryParse(parts[3], out var stime))
            {
                correctReads++;
                procStat.SystemTime = stime;
            }

            if (ulong.TryParse(parts[4], out var itime))
            {
                correctReads++;
                procStat.IdleTime = itime;
            }
        }

        if (useDotnetCpuCount)
            procStat.CpuCount = Environment.ProcessorCount; //NOTE function in linux, not constant
        else
            procStat.CpuCount = systemStatReader.ReadLines().Count(line => line.StartsWith("cpu")) - 1;
        return correctReads == 4;
    }
#endif

    public void Dispose()
    {
        systemStatReader?.Dispose();
    }
}