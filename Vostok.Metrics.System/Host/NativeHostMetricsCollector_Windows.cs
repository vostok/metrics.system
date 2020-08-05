using System;
using System.Linq;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;
using Vostok.Sys.Metrics.PerfCounters;

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Windows
    {
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector();
        private readonly IPerformanceCounter<Observation<NetworkUsage>[]> networkUsageCounter = PerformanceCounterFactory.Default
           .Create<NetworkUsage>()
           .AddCounter(
                "Network Interface",
                "Bytes Sent/sec",
                (context, value) => context.Result.NetworkSentBytesPerSecond = value)
           .AddCounter(
                "Network Interface",
                "Bytes Received/sec",
                (context, value) => context.Result.NetworkReceivedBytesPerSecond = value)
           .BuildForMultipleInstances("*");

        public void Collect(HostMetrics metrics)
        {
            CollectCpuUtilization(metrics);
            CollectMemoryMetrics(metrics);
            CollectNetworkUsage(metrics);
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

        private void CollectNetworkUsage(HostMetrics metrics)
        {
            try
            {
                var networkUsageInfo = networkUsageCounter.Observe();

                metrics.NetworkSentBytesPerSecond = networkUsageInfo
                   .Select(x => x.Value.NetworkSentBytesPerSecond)
                   .Sum();

                metrics.NetworkReceivedBytesPerSecond = networkUsageInfo
                   .Select(x => x.Value.NetworkReceivedBytesPerSecond)
                   .Sum();
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private class NetworkUsage
        {
            public double NetworkSentBytesPerSecond { get; set; }
            public double NetworkReceivedBytesPerSecond { get; set; }
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