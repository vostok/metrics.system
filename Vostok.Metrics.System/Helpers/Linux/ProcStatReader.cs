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
    private const string file = "/proc/stat";

    public ProcStatReader()
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(file);
#else
        reader = new ReusableFileReader(file);
#endif
    }

    public ProcStatReader(Stream procStatStream)
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(procStatStream);
#else
        reader = new ReusableFileReader(procStatStream);
#endif
    }

#if NET6_0_OR_GREATER
    private readonly SpanReusableFileReader reader;
#else
    private readonly ReusableFileReader reader; //2.7k
#endif

    public bool TryRead(out ProcStat value)
    {
        value = new ProcStat();
#if NET6_0_OR_GREATER
        reader.Reset();

        if (!reader.TryReadLine(out var line)
            || !TryParseTimes(line, ref value))
            return false;
        return true;
#else
        return ParseNotOptimized(ref value);
#endif
    }

#if NET6_0_OR_GREATER

    private static bool TryParseTimes(ReadOnlySpan<char> line, ref ProcStat value)
    {
        var e = ProcFsParserHelper.EnumerateTokens(line);

        if (!e.TryMove(0) || !e.Token.SequenceEqual("cpu"))
            return false;

        if (!e.TryMove(1) || !ulong.TryParse(e.Token, out value.UserTime))
            return false;
        if (!e.TryMove(2) || !ulong.TryParse(e.Token, out value.NicedTime))
            return false;
        if (!e.TryMove(3) || !ulong.TryParse(e.Token, out value.SystemTime))
            return false;
        if (!e.TryMove(4) || !ulong.TryParse(e.Token, out value.IdleTime))
            return false;
        return true;
    }
#else
    private bool ParseNotOptimized(ref ProcStat value)
    {
        var correctReads = 0;
        if (FileParser.TrySplitLine(reader.ReadFirstLine(), 5, out var parts) && parts[0] == "cpu")
        {
            if (ulong.TryParse(parts[1], out var utime))
            {
                correctReads++;
                value.UserTime = utime;
            }

            if (ulong.TryParse(parts[2], out value.NicedTime))
                correctReads++;

            if (ulong.TryParse(parts[3], out var stime))
            {
                correctReads++;
                value.SystemTime = stime;
            }

            if (ulong.TryParse(parts[4], out var itime))
            {
                correctReads++;
                value.IdleTime = itime;
            }
        }

        return correctReads == 4;
    }
#endif

    public void Dispose()
    {
        reader?.Dispose();
    }
}