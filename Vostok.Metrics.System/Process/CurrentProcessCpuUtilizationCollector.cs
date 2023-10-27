using System;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    internal class CpuUtilizationCollector
    {
        private ulong previousSystemTime;
        private ulong previousProcessTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="systemTime"></param>
        /// <param name="processTime"></param>
        /// <param name="systemCores">expect host cores if availbale</param>
        public void Collect(CurrentProcessMetrics metrics, ulong systemTime, ulong processTime, int systemCores)
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
                var cores = systemCores;

                metrics.CpuUtilizedCores = (cores * processTimeDiff / systemTimeDiff).Clamp(0, cores);
                metrics.CpuUtilizedFraction = (processTimeDiff / systemTimeDiff).Clamp(0, 1);
            }

            previousSystemTime = systemTime;
            previousProcessTime = processTime;
        }
    }
}