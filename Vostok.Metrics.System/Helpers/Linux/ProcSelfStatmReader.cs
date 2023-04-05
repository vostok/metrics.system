using System;
using System.IO;

namespace Vostok.Metrics.System.Helpers.Linux;

internal class ProcSelfStatmReader : IDisposable
{
    internal int pageSize;
#if NET6_0_OR_GREATER
    private readonly SpanReusableFileReader statmReader;
#else
    private readonly ReusableFileReader statmReader;
#endif

    public ProcSelfStatmReader()
    {
        pageSize = -2;
#if NET6_0_OR_GREATER
        statmReader = new SpanReusableFileReader("/proc/self/statm"); //can read from /proc/self/stat but it's slower
#else
        statmReader = new ReusableFileReader("/proc/self/statm");
#endif
    }

    public ProcSelfStatmReader(Stream stream)
    {
        pageSize = -2;
#if NET6_0_OR_GREATER
        statmReader = new SpanReusableFileReader(stream); //can read from /proc/self/stat but it's slower
#else
        statmReader = new ReusableFileReader(stream);
#endif
    }


    public bool TryRead(out ProcSelfStatm value)
    {
        value = new ProcSelfStatm();
        if (pageSize == -2)
        {
            pageSize = (int)LibcApi.sysconf(LibcApi._SC_PAGESIZE);
            if (pageSize == -1) //error
                pageSize = 4096;
        }

#if NET6_0_OR_GREATER
        statmReader.Reset();

        if (!statmReader.TryReadLine(out var line))
            return false;
        var e = ProcFsParserHelper.EnumerateTokens(line);

        if (!e.TryMove(1) || !long.TryParse(e.Token, out value.PrivateRss))
            return false;
        if (!e.TryMove(2) || !long.TryParse(e.Token, out var shared))
            return false;


        if (!e.TryMove(5) || !long.TryParse(e.Token, out value.DataSize)) //+ stack size
            return false;

        value.PrivateRss -= shared;
        value.PrivateRss *= pageSize;
        value.DataSize *= pageSize;

        return true;
#else
        return ReadNotOptimized(ref value);
#endif
    }

#if NET6_0_OR_GREATER
#else
    private bool ReadNotOptimized(ref ProcSelfStatm value)
    {
        var line = statmReader.ReadFirstLine();
        if (!FileParser.TrySplitLine(line, 6, out var parts))
            return false;

        if (!long.TryParse(parts[1], out value.PrivateRss))
            return false;
        if (!long.TryParse(parts[2], out var shared))
            return false;

        if (!long.TryParse(parts[5], out value.DataSize))
            return false;

        value.PrivateRss -= shared;
        value.PrivateRss *= pageSize;
        value.DataSize *= pageSize;
        return true;
    }
#endif

    public void Dispose()
    {
        statmReader?.Dispose();
    }
}