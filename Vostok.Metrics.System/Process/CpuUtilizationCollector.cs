using System;

namespace Vostok.Metrics.System.Process
{
    internal class CpuUtilizationCollector
    {
        private static readonly int CoresCount = Environment.ProcessorCount;

        private ulong previousSystemTime;
        private ulong previousProcessTime;

        public void Collect(CurrentProcessMetrics metrics, ulong systemTime, ulong processTime)
        {
            var systemTimeDiff = (double) systemTime - previousSystemTime;
            var processTimeDiff = (double) processTime - previousProcessTime;

            if (previousSystemTime == 0 || systemTimeDiff <= 0)
            {
                metrics.CpuUtilizedCores = 0d;
                metrics.CpuUtilizedFraction = 0d;
            }
            else
            {
                metrics.CpuUtilizedCores = Clamp(0, CoresCount, CoresCount * processTimeDiff / systemTimeDiff);
                metrics.CpuUtilizedFraction = Clamp(0, 1, processTimeDiff / systemTimeDiff);
            }

            previousSystemTime = systemTime;
            previousProcessTime = processTime;
        }

        private static double Clamp(double min, double max, double value)
            => Math.Max(min, Math.Min(value, max));
    }
}
