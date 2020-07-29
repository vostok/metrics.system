using System;

namespace Vostok.Metrics.System.Host
{
    internal class HostCpuUtilizationCollector
    {
        private static readonly int CoresCount = Environment.ProcessorCount;

        private ulong previousSystemTime;
        private ulong previousIdleTime;

        public void Collect(HostMetrics metrics, ulong systemTime, ulong idleTime)
        {
            var systemTimeDiff = (double) systemTime - previousSystemTime;
            var idleTimeDiff = (double) idleTime - previousIdleTime;
            var spentTimeDiff = 1 - idleTimeDiff / systemTimeDiff;

            if (previousSystemTime == 0 || systemTimeDiff <= 0)
            {
                metrics.CpuUtilizedCores = 0d;
                metrics.CpuUtilizedFraction = 0d;
            }
            else
            {
                metrics.CpuUtilizedCores = Clamp(0, CoresCount, CoresCount * spentTimeDiff);
                metrics.CpuUtilizedFraction = Clamp(0, 1, spentTimeDiff);
            }

            previousSystemTime = systemTime;
            previousIdleTime = idleTime;
        }

        private static double Clamp(double min, double max, double value)
            => Math.Max(min, Math.Min(value, max));
    }
}