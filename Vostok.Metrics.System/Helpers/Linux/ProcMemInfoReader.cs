using System;
using System.IO;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

#else
#endif

namespace Vostok.Metrics.System.Helpers.Linux;

internal class ProcMemInfoReader : IDisposable
{
    private const string file = "/proc/meminfo";

    public ProcMemInfoReader()
    {
#if NET6_0_OR_GREATER
        reader = new SpanReusableFileReader(file);
#else
        reader = new ReusableFileReader(file);
#endif
    }

    public ProcMemInfoReader(Stream stream)
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

    public bool TryRead(out ProcMemInfo value)
    {
        value = new ProcMemInfo();
#if NET6_0_OR_GREATER
        reader.Reset();

        //note each line can contain multiple spaces as delimiter
        //MemTotal:       65749580 kB

        const int fields = 5;
        ulong mask = (1 << fields) - 1; //fields bits set to 1

        while (mask != 0 && reader.TryReadLine(out var line))
        {
            if (!TryParseLine(line, "MemTotal:", ref mask, 0, ref value.TotalMemory))
                return false;
            if (!TryParseLine(line, "MemAvailable:", ref mask, 1, ref value.AvailableMemory))
                return false;
            if (!TryParseLine(line, "Cached:", ref mask, 2, ref value.CacheMemory))
                return false;
            if (!TryParseLine(line, "Slab:", ref mask, 3, ref value.KernelMemory))
                return false;
            if (!TryParseLine(line, "MemFree:", ref mask, 4, ref value.FreeMemory))
                return false;
        }

        ConvertToBytes(ref value);

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
            var e = ProcFsParserHelper.EnumerateTokens(line.Slice(key.Length));
            if (!e.TryMove(0) || !long.TryParse(e.Token, out result))
                return false;
        }

        return true;
    }
#else
    private bool ParseNotOptimized(ref ProcMemInfo result)
    {
        foreach (var line in reader.ReadLines())
        {
            if (FileParser.TryParseLong(line, "MemTotal", out var memTotal))
                result.TotalMemory = memTotal * 1024;

            if (FileParser.TryParseLong(line, "MemAvailable", out var memAvailable))
                result.AvailableMemory = memAvailable * 1024;

            if (FileParser.TryParseLong(line, "Cached", out var memCached))
                result.CacheMemory = memCached * 1024;

            if (FileParser.TryParseLong(line, "Slab", out var memKernel))
                result.KernelMemory = memKernel * 1024;

            if (FileParser.TryParseLong(line, "MemFree", out var memFree))
                result.FreeMemory = memFree * 1024;
        }

        return true;
    }

#endif

    public void Dispose()
    {
        reader?.Dispose();
    }

    private static void ConvertToBytes(ref ProcMemInfo result)
    {
        //note /proc/meminfo shows always print in 'Kb' according to kernel source code
        result.TotalMemory *= 1024;
        result.AvailableMemory *= 1024;
        result.CacheMemory *= 1024;
        result.KernelMemory *= 1024;
        result.FreeMemory *= 1024;
    }
}