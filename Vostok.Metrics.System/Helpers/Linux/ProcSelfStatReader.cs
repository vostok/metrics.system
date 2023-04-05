using System;
using System.IO;

namespace Vostok.Metrics.System.Helpers.Linux;

internal readonly struct ProcSelfStatReader : IDisposable
{
    //see https://man7.org/linux/man-pages/man5/proc.5.html
    //proc/[pid]/stat
    //'comm' field CAN contain any chars (spaces, line break, '()') - should correctly skip them

    //example from real system::
    //2965399 ((z ) c) S 2964851 2965399 2842648 34816 2965399 4194304 164 0 0 0 0 0 0 0 20 0 1 0 196909631 7057408 803 18446744073709551615 94924241006592 94924241913557 140723843549632 0 0 0 65536 4 65538 1 0 0 17 4 0 0 0 0 0 94924242144496 94924242191876 94924252536832 140723843553013 140723843553040 140723843553040 140723843555300 0
#if NET6_0_OR_GREATER
    private readonly SpanReusableFileReader reader;
#else
    private readonly ReusableFileReader reader;
#endif
    
    public ProcSelfStatReader(Stream stream)
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(stream);
#else
        reader = new ReusableFileReader(stream);
#endif
    }

    public ProcSelfStatReader()
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader("/proc/self/stat");
#else
        reader = new ReusableFileReader("/proc/self/stat");
#endif
    }

    public bool TryRead(out ProcSelfStat value)
    {
        value = new ProcSelfStat();

#if NET6_0_OR_GREATER
        reader.Reset();

        if (!reader.TryReadLine(out var line))
            return false;
        /*
        see source code "fs\proc\array.c"
        do_task_stat function
        
            seq_put_decimal_ull(m, "", pid_nr_ns(pid, ns));
            seq_puts(m, " (");
            proc_task_name(m, task, false); //<--- comm field here. false means NO ESCAPING
            seq_puts(m, ") ");
            seq_putc(m, state);
        ...
           print other integers. no chars, no strings
         */

        var start = line.LastIndexOf(')') + 2; //'comm' field is ANY chars(max 16) surrounded by '(' and ')'
        //var start = line.LastIndexOf(") ") + 2; //'comm' field is ANY chars(max 16) surrounded by '(' and ')'

        const int tokensSkipped = 2;
        line = line.Slice(start); //now truncate 2 first fields

        var e = ProcFsParserHelper.EnumerateTokens(line);
        //numbers are given from 1, not 0
        //(14) utime  %lu
        if (!e.TryMove(14 - 1 - tokensSkipped) || !ulong.TryParse(e.Token, out value.utime))
            return false;
        //(15) stime  %lu
        if (!e.TryMove(15 - 1 - tokensSkipped) || !ulong.TryParse(e.Token, out value.stime))
            return false;
        return true;
#else
        return ReadNotOptimized(ref value);
#endif
    }
#if NET6_0_OR_GREATER
#else
    private bool ReadNotOptimized(ref ProcSelfStat value)
    {
        //bug: comm field (index=1) can contain whitespaces!!
        if (!FileParser.TrySplitLine(reader.ReadFirstLine(), 15, out var parts))
            return false;
        if (ulong.TryParse(parts[13], out var utime))
            value.utime = utime;

        if (ulong.TryParse(parts[14], out var stime))
            value.stime = stime;

        return true;
    }
#endif

    public void Dispose()
    {
        reader?.Dispose();
    }
}