using System;
using System.IO;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

#else
#endif

namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcVmStat
{
    public long pgmajfault;
}

internal class ProcVmStatReader : IDisposable
{
    private const string file = "/proc/vmstat";

    public ProcVmStatReader()
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(file);
#else
        reader = new ReusableFileReader(file);
#endif
    }

    public ProcVmStatReader(Stream stream)
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(stream);
#else
        reader = new ReusableFileReader(stream);
#endif
    }

#if NET6_0_OR_GREATER
    private readonly SpanReusableFileReader reader;
#else
    private readonly ReusableFileReader reader; //2.7k
#endif

    public bool TryRead(out ProcVmStat value)
    {
        value = new ProcVmStat();
#if NET6_0_OR_GREATER
        reader.Reset();

        //format: for each line
        //field_name long_val
        //1 space is separator

        const int fields = 1;
        ulong mask = (1 << fields) - 1; //fields bits set to 1

        while (mask != 0 && reader.TryReadLine(out var line))
        {
            if (!TryParseLine(line, "pgmajfault" + ' ', ref mask, 0, ref value.pgmajfault))
                return false;
        }

        return true;
#else
        return ParseNotOptimized(ref value);
#endif
    }

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static bool TryParseLine(ReadOnlySpan<char> line, string key, ref ulong mask, byte bit, ref long result)
    {
        if ((mask & (1u << bit)) != 0 && line.StartsWith(key))
        {
            mask ^= 1u << bit;
            if (!long.TryParse(line.Slice(key.Length), out result))
                return false;
        }

        return true;
    }
#else
    private bool ParseNotOptimized(ref ProcVmStat value)
    {
        foreach (var line in reader.ReadLines())
        {
            if (FileParser.TryParseLong(line, "pgmajfault", out var faults))
            {
                value.pgmajfault = faults;
                return true;
            }
        }

        return true;
    }
#endif

    public void Dispose()
    {
        reader?.Dispose();
    }
}