using System;
using System.IO;

namespace Vostok.Metrics.System.Helpers.Linux;

#if NET6_0_OR_GREATER
internal class SpanReusableFileReader : IDisposable
{
    private readonly string path;
    private volatile StreamLinesReader reader;

    public SpanReusableFileReader(string path)
        => this.path = path;

    public SpanReusableFileReader(Stream stream)
    {
        reader = new StreamLinesReader(stream, ownsStream: true);
    }

    public bool TryReadLine(out ReadOnlySpan<char> result)
    {
        return reader.TryReadLine(out result);
    }

    public void Dispose()
        => reader?.Dispose();

    public void Reset()
    {
        if (reader == null)
        {
            // note (kungurtsev, 23.11.2021): buffer size = 1 for disabling FileStream buffering strategy
            reader = new StreamLinesReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1), ownsStream: true);
            return;
        }

        reader.ResetToZero();
    }
}
#endif