using System;
using Vostok.Metrics.System.Helpers;

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
                metrics.CpuUtilizedCores = (CoresCount * processTimeDiff / systemTimeDiff).Clamp(0, CoresCount);
                metrics.CpuUtilizedFraction = (processTimeDiff / systemTimeDiff).Clamp(0, 1);
            }

            previousSystemTime = systemTime;
            previousProcessTime = processTime;
        }
    }
}