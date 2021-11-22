using System;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class HostCpuUtilizationCollector
    {
        private readonly Func<int?> coresCountProvider;
        private int previousCoresCount = Environment.ProcessorCount;
        private ulong previousSystemTime;
        private ulong previousIdleTime;

        public HostCpuUtilizationCollector(Func<int?> coresCountProvider)
        {
            this.coresCountProvider = coresCountProvider;
        }

        public void Collect(HostMetrics metrics, ulong systemTime, ulong idleTime)
        {
            var systemTimeDiff = (double)systemTime - previousSystemTime;
            var idleTimeDiff = (double)idleTime - previousIdleTime;
            var spentTimeDiff = 1 - idleTimeDiff / systemTimeDiff;

            metrics.CpuTotalCores = previousCoresCount = coresCountProvider() ?? previousCoresCount;

            if (previousSystemTime == 0 || systemTimeDiff <= 0)
            {
                metrics.CpuUtilizedCores = 0d;
                metrics.CpuUtilizedFraction = 0d;
            }
            else
            {
                metrics.CpuUtilizedCores = (metrics.CpuTotalCores * spentTimeDiff).Clamp(0, metrics.CpuTotalCores);
                metrics.CpuUtilizedFraction = spentTimeDiff.Clamp(0, 1);
            }

            previousSystemTime = systemTime;
            previousIdleTime = idleTime;
        }
    }
}