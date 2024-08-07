﻿using System;

namespace Vostok.Metrics.System.Helpers
{
    internal static class FileParser
    {
        public static bool TrySplitLine(string line, int minParts, out string[] parts)
            => (parts = line?.Split(null as char[], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Length >= minParts;

        public static bool TryParseLong(string line, string name, out long value)
        {
            value = 0;

            return line.StartsWith(name) && TrySplitLine(line, 2, out var parts) && long.TryParse(parts[1], out value);
        }

        public static bool TryParseLongRef(string line, string name, ref long value)
        {
            return line.StartsWith(name) && TrySplitLine(line, 2, out var parts) && long.TryParse(parts[1], out value);
        }
    }
}