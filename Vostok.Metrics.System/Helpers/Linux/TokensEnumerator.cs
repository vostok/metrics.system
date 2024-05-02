using System;
#if NET6_0_OR_GREATER
using Vostok.Commons.Helpers.Spans;
#endif


namespace Vostok.Metrics.System.Helpers.Linux;

#if NET6_0_OR_GREATER
internal ref struct TokensEnumerator
{
    private static readonly char[] separators = {' '}; //note судя по коду линукс, только пробел разделитель в procFS

    private SpanSplitter.Enumerator enumerator;
    private int currentPosition;

    public TokensEnumerator(ReadOnlySpan<char> source)
    {
        enumerator = source.Split(separators, true).GetEnumerator(); //general options: skip empty tokens, separator = ' '
        currentPosition = -1;
    }

    public ReadOnlySpan<char> Token => enumerator.Current;

    /// <summary>
    /// move forward to absolute token index (from 0)
    /// </summary>
    /// <param name="tokenPosition"></param>
    /// <returns></returns>
    public bool TryMove(int tokenPosition)
    {
        if (tokenPosition < currentPosition)
            FailInvalidUsage();
        for (; currentPosition < tokenPosition; currentPosition++)
        {
            if (!enumerator.MoveNext())
                return false;
        }

        return true;
    }

    private void FailInvalidUsage()
    {
        throw new NotSupportedException("Cant move backwards");
    }
}
#endif