namespace Vostok.Metrics.System.Helpers
{
    internal static class SizeFormatter
    {
        private const long Kilobyte = 1024;
        private const long Megabyte = Kilobyte * Kilobyte;
        private const long Gigabyte = Megabyte * Kilobyte;
        private const long Terabyte = Gigabyte * Kilobyte;
        private const long Petabyte = Terabyte * Kilobyte;

        public static string Format(long size)
            => TryFormat(size, Petabyte, "PB") ??
               TryFormat(size, Terabyte, "TB") ??
               TryFormat(size, Gigabyte, "GB") ??
               TryFormat(size, Megabyte, "MB") ??
               TryFormat(size, Kilobyte, "KB") ??
               $"{size} B";

        private static string TryFormat(long size, long unit, string suffix)
        {
            var units = size / (double)unit;
            if (units >= 1)
                return $"{units:0.##} {suffix}";

            return null;
        }
    }
}
