using System;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Windows
    {
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector();
        private readonly DiskSpaceCollector diskSpaceCollector = new DiskSpaceCollector();

        public void Collect(HostMetrics metrics)
        {
            CollectCpuUtilization(metrics);
            CollectMemoryMetrics(metrics);
            diskSpaceCollector.Collect(metrics);
        }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetPerformanceInfo(
            [Out] out PERFORMANCE_INFORMATION ppsmemCounters,
            [In] int cb);

        private void CollectCpuUtilization(HostMetrics metrics)
        {
            try
            {
                if (!WinMetricsCollectorHelper.GetSystemTimes(out var idleTime, out var systemKernel, out var systemUser))
                    WinMetricsCollectorHelper.ThrowOnError();

                var systemTime = systemKernel.ToUInt64() + systemUser.ToUInt64();

                cpuCollector.Collect(metrics, systemTime, idleTime.ToUInt64());
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private unsafe void CollectMemoryMetrics(HostMetrics metrics)
        {
            try
            {
                if (!GetPerformanceInfo(out var perfInfo, sizeof(PERFORMANCE_INFORMATION)))
                    WinMetricsCollectorHelper.ThrowOnError();

                metrics.MemoryAvailable = (long) perfInfo.PhysicalAvailable * (long) perfInfo.PageSize;
                metrics.MemoryCached = (long) perfInfo.SystemCache * (long) perfInfo.PageSize;
                metrics.MemoryKernel = (long) perfInfo.KernelTotal * (long) perfInfo.PageSize;
                metrics.MemoryTotal = (long) perfInfo.PhysicalTotal * (long) perfInfo.PageSize;

                metrics.ProcessCount = (int) perfInfo.ProcessCount;
                metrics.ThreadCount = (int) perfInfo.ThreadCount;
                metrics.HandleCount = (int) perfInfo.HandleCount;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PERFORMANCE_INFORMATION
        {
            public readonly int cb;
            public readonly UIntPtr CommitTotal;
            public readonly UIntPtr CommitLimit;
            public readonly UIntPtr CommitPeak;
            public readonly UIntPtr PhysicalTotal;
            public readonly UIntPtr PhysicalAvailable;
            public readonly UIntPtr SystemCache;
            public readonly UIntPtr KernelTotal;
            public readonly UIntPtr KernelPaged;
            public readonly UIntPtr KernelNonpaged;
            public readonly UIntPtr PageSize;
            public readonly uint HandleCount;
            public readonly uint ProcessCount;
            public readonly uint ThreadCount;
        }
    }
}