using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;
using Vostok.Sys.Metrics.PerfCounters;

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Windows : IDisposable
    {
        private readonly HostMetricsSettings settings;
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector(GetProcessorCount);

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
            .AddCounter(
                "Network Interface",
                "Current Bandwidth",
                (context, value) => context.Result.NetworkCurrentBandwidthBitsPerSecond = value)
            .BuildForMultipleInstances("*");

        private readonly IPerformanceCounter<MemoryInfo> memoryInfoCounter = PerformanceCounterFactory.Default
            .Create<MemoryInfo>()
            .AddCounter(
                "Memory",
                "Free & Zero Page List Bytes",
                (context, value) => context.Result.MemoryFreeBytes = (long)value)
            .AddCounter(
                "Memory",
                "Page Faults/sec",
                (context, value) => context.Result.PageFaultsPerSecond = (long)value)
            .Build();

        private readonly DiskUsageCollector_Windows diskUsageCollector = new DiskUsageCollector_Windows();

        public NativeHostMetricsCollector_Windows(HostMetricsSettings settings)
            => this.settings = settings;

        public void Dispose()
        {
            networkUsageCounter.Dispose();
            memoryInfoCounter.Dispose();
            diskUsageCollector.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            if (settings.CollectCpuMetrics)
                CollectCpuUtilization(metrics);

            if (settings.CollectMemoryMetrics)
                CollectMemoryMetrics(metrics);

            if (settings.CollectNetworkUsageMetrics)
                CollectNetworkUsage(metrics);

            if (settings.CollectDiskUsageMetrics)
            {
                var diskUsage = diskUsageCollector.Collect();
                if (diskUsage != null)
                    metrics.DisksUsageInfo = diskUsage;
            }
        }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetPerformanceInfo(
            [Out] out PERFORMANCE_INFORMATION ppsmemCounters,
            [In] int cb);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetNativeSystemInfo([Out] out SYSTEM_INFO info);

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

                metrics.MemoryAvailable = (long)perfInfo.PhysicalAvailable * (long)perfInfo.PageSize;
                metrics.MemoryCached = (long)perfInfo.SystemCache * (long)perfInfo.PageSize;
                metrics.MemoryKernel = (long)perfInfo.KernelTotal * (long)perfInfo.PageSize;
                metrics.MemoryTotal = (long)perfInfo.PhysicalTotal * (long)perfInfo.PageSize;

                metrics.ProcessCount = (int)perfInfo.ProcessCount;
                metrics.ThreadCount = (int)perfInfo.ThreadCount;
                metrics.HandleCount = (int)perfInfo.HandleCount;

                var memoryInfo = memoryInfoCounter.Observe();

                metrics.MemoryFree = memoryInfo.MemoryFreeBytes;
                metrics.PageFaultsPerSecond = memoryInfo.PageFaultsPerSecond;
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
                var networkInterfacesUsageInfo = new Dictionary<string, NetworkInterfaceUsageInfo>();

                foreach (var networkUsageObservation in networkUsageCounter.Observe())
                {
                    var result = new NetworkInterfaceUsageInfo
                    {
                        InterfaceName = networkUsageObservation.Instance,
                        ReceivedBytesPerSecond = (long)networkUsageObservation.Value.NetworkReceivedBytesPerSecond,
                        SentBytesPerSecond = (long)networkUsageObservation.Value.NetworkSentBytesPerSecond,
                        BandwidthBytesPerSecond = (long)(networkUsageObservation.Value.NetworkCurrentBandwidthBitsPerSecond / 8d)
                    };

                    networkInterfacesUsageInfo[networkUsageObservation.Instance] = result;
                }

                metrics.NetworkInterfacesUsageInfo = networkInterfacesUsageInfo;

                metrics.NetworkSentBytesPerSecond = networkInterfacesUsageInfo.Values.Sum(x => x.SentBytesPerSecond);
                metrics.NetworkReceivedBytesPerSecond = networkInterfacesUsageInfo.Values.Sum(x => x.ReceivedBytesPerSecond);
                metrics.NetworkBandwidthBytesPerSecond = networkInterfacesUsageInfo.Values.Sum(x => x.BandwidthBytesPerSecond);
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private static int? GetProcessorCount()
        {
            {
                try
                {
                    GetNativeSystemInfo(out var info);
                    WinMetricsCollectorHelper.ThrowOnError();
                    return (int)info.NumberOfProcessors;
                }
                catch (Exception error)
                {
                    InternalErrorLogger.Warn(error);
                    return null;
                }
            }
        }

        private class MemoryInfo
        {
            public long PageFaultsPerSecond { get; set; }
            public long MemoryFreeBytes { get; set; }
        }

        private class NetworkUsage
        {
            public double NetworkSentBytesPerSecond { get; set; }
            public double NetworkReceivedBytesPerSecond { get; set; }
            public double NetworkCurrentBandwidthBitsPerSecond { get; set; }
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

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public readonly ushort ProcessorArchitecture;
            public readonly ushort Reserved;
            public readonly uint PageSize;
            public readonly IntPtr MinimumApplicationAddress;
            public readonly IntPtr MaximumApplicationAddress;
            public readonly IntPtr ActiveProcessorMask;
            public readonly uint NumberOfProcessors;
            public readonly uint ProcessorType;
            public readonly uint AllocationGranularity;
            public readonly ushort ProcessorLevel;
            public readonly ushort ProcessorRevision;
        }
    }
}