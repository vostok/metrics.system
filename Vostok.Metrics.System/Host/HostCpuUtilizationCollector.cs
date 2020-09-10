using System;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class HostCpuUtilizationCollector
    {
        private static readonly int CoresCount = Environment.ProcessorCount;

        private ulong previousSystemTime;
        private ulong previousIdleTime;

        public void Collect(HostMetrics metrics, ulong systemTime, ulong idleTime)
        {
            var systemTimeDiff = (double)systemTime - previousSystemTime;
            var idleTimeDiff = (double)idleTime - previousIdleTime;
            var spentTimeDiff = 1 - idleTimeDiff / systemTimeDiff;

            metrics.CpuTotalCores = Environment.ProcessorCount;

            if (previousSystemTime == 0 || systemTimeDiff <= 0)
            {
                metrics.CpuUtilizedCores = 0d;
                metrics.CpuUtilizedFraction = 0d;
            }
            else
            {
                metrics.CpuUtilizedCores = (CoresCount * spentTimeDiff).Clamp(0, CoresCount);
                metrics.CpuUtilizedFraction = spentTimeDiff.Clamp(0, 1);
            }

            previousSystemTime = systemTime;
            previousIdleTime = idleTime;
        }
    }
}