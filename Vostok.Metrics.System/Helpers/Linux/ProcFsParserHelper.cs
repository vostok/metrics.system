using System;

namespace Vostok.Metrics.System.Helpers.Linux;

internal static class ProcFsParserHelper
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// enumerate tokens in line. token is sequence of chars without spaces
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static TokensEnumerator EnumerateTokens(ReadOnlySpan<char> line)
    {
        return new TokensEnumerator(line);
    }

#endif
}