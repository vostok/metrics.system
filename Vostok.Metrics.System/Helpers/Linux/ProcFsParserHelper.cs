using System;

namespace Vostok.Metrics.System.Helpers;

internal static class ProcFsParserHelper
{
    private static readonly char[] separators = {' '}; //todo судя по коду линукс, только пробел разделитель

#if NET6_0_OR_GREATER
    public static SpanSplitter.Enumerable EnumerateTokens(
        this ReadOnlySpan<char> source)
    {
        return source.Split(separators, true);
    }

    public static SpanSplitter.Enumerator GetTokensEnumerator(
        this ReadOnlySpan<char> source)
    {
        return source.Split(separators, true).GetEnumerator();
    }
    public static TokensEmum22 GetTokensEnumerator2(
        this ReadOnlySpan<char> source)
    {
        return new TokensEmum22(source);
    }

#endif
}
#if NET6_0_OR_GREATER
internal ref struct TokensEmum22
{
    public TokensEmum22(ReadOnlySpan<char> source)
    {
        enumerator = ProcFsParserHelper.EnumerateTokens(source).GetEnumerator();
        currrentPosition = -1;
    }

    private SpanSplitter.Enumerator enumerator;
    private int currrentPosition;
    
    /// <summary>
    /// move forward to absolute token index (from 0)
    /// </summary>
    /// <param name="tokenPosition"></param>
    /// <returns></returns>
    public bool TryMove(int tokenPosition)
    {
        if (tokenPosition < currrentPosition)
            FailInvalidUsage();
        for (; currrentPosition < tokenPosition; currrentPosition++)
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

    public ReadOnlySpan<char> Token => enumerator.Current;
}
#endif