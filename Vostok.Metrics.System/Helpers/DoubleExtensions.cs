using System;

namespace Vostok.Metrics.System.Helpers
{
    internal static class DoubleExtensions
    {
        public static double Clamp(this double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }
    }
}