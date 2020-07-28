using System;

namespace Vostok.Metrics.System.Helpers
{
    public static class FileParser
    {
        public static bool TrySplitLine(string line, int minParts, out string[] parts)
            => (parts = line?.Split(null as char[], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Length >= minParts;

        public static bool TryParse(string line, string name, out long value)
        {
            value = 0;

            return line.StartsWith(name) && TrySplitLine(line, 2, out var parts) && long.TryParse(parts[1], out value);
        }
    }
}