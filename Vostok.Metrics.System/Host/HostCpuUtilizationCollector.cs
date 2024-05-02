using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class HostCpuUtilizationCollector
    {
        private ulong previousSystemTime;
        private ulong previousIdleTime;
        private ulong previousKernelTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="systemTime">total time passed(sum for all cpus)</param>
        /// <param name="idleTime">sum of idle times for all cpus</param>
        /// <param name="cpuCount">processor count</param>
        /// <param name="kernelTime">time spent in kernel mode</param>
        public void Collect(HostMetrics metrics, ulong systemTime, ulong idleTime, ulong kernelTime, int cpuCount)
        {
            var systemTimeDiff = (double)systemTime - previousSystemTime;

            metrics.CpuTotalCores = cpuCount;

            if (previousSystemTime == 0 || systemTimeDiff <= 0)
            {
                metrics.CpuUtilizedCores = 0d;
                metrics.CpuUtilizedFraction = 0d;
                metrics.CpuUtilizedFractionInKernel = 0d;
            }
            else
            {
                var idleTimeDiff = (double)idleTime - previousIdleTime;
                var spentTimeDiff = 1 - idleTimeDiff / systemTimeDiff;

                metrics.CpuUtilizedCores = (metrics.CpuTotalCores * spentTimeDiff).Clamp(0, metrics.CpuTotalCores);
                metrics.CpuUtilizedFraction = spentTimeDiff.Clamp(0, 1);

                var kernelTimeDiff = (double)kernelTime - previousKernelTime;
                metrics.CpuUtilizedFractionInKernel = (kernelTimeDiff/systemTimeDiff).Clamp(0, 1);
            }

            previousSystemTime = systemTime;
            previousIdleTime = idleTime;
            previousKernelTime = kernelTime;
        }
    }
}