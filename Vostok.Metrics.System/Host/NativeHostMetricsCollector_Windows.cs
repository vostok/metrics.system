using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;
using Vostok.Sys.Metrics.PerfCounters;

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Windows : IDisposable
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
                (context, value) => context.Result.MemoryFreeBytes = (long) value)
           .AddCounter(
                "Memory",
                "Page Faults/sec",
                (context, value) => context.Result.PageFaultsPerSecond = (long) value)
           .Build();

        private readonly IPerformanceCounter<Observation<DiskUsage>[]> diskUsageCounter = PerformanceCounterFactory.Default
           .Create<DiskUsage>()
           .AddCounter("LogicalDisk", "% Idle Time", (context, value) => context.Result.IdleTimePercent = value.Clamp(0, 100))
           .AddCounter(
                "LogicalDisk",
                "Avg. Disk sec/Read",
                (context, value) => context.Result.ReadAverageSecondsLatency = value)
           .AddCounter(
                "LogicalDisk",
                "Avg. Disk sec/Write",
                (context, value) => context.Result.WriteAverageSecondsLatency = value)
           .AddCounter("LogicalDisk", "Disk Reads/sec", (context, value) => context.Result.DiskReadsPerSecond = (long) value)
           .AddCounter("LogicalDisk", "Disk Writes/sec", (context, value) => context.Result.DiskWritesPerSecond = (long) value)
           .AddCounter(
                "LogicalDisk",
                "Current Disk Queue Length",
                (context, value) => context.Result.CurrentQueueLength = (long) value)
           .AddCounter("LogicalDisk", "Disk Read Bytes/sec", (context, value) => context.Result.BytesReadPerSecond = (long) value)
           .AddCounter("LogicalDisk", "Disk Write Bytes/sec", (context, value) => context.Result.BytesWrittenPerSecond = (long) value)
           .BuildForMultipleInstances("*:");

        public void Dispose()
        {
            networkUsageCounter.Dispose();
            memoryInfoCounter.Dispose();
            diskUsageCounter.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            CollectCpuUtilization(metrics);
            CollectMemoryMetrics(metrics);
            CollectNetworkUsage(metrics);
            CollectDisksUsage(metrics);
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
                        ReceivedBytesPerSecond = (long) networkUsageObservation.Value.NetworkReceivedBytesPerSecond,
                        SentBytesPerSecond = (long) networkUsageObservation.Value.NetworkSentBytesPerSecond,
                        BandwidthBytesPerSecond = (long) (networkUsageObservation.Value.NetworkCurrentBandwidthBitsPerSecond / 8d)
                    };

                    networkInterfacesUsageInfo[networkUsageObservation.Instance] = result;
                }

                metrics.NetworkInterfacesUsageInfo = networkInterfacesUsageInfo;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private void CollectDisksUsage(HostMetrics metrics)
        {
            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>();

            try
            {
                foreach (var diskUsageInfo in diskUsageCounter.Observe())
                {
                    var result = new DiskUsageInfo
                    {
                        DiskName = diskUsageInfo.Instance.Replace(":", string.Empty),
                        ReadAverageMsLatency = (long) (diskUsageInfo.Value.ReadAverageSecondsLatency * 1000),
                        WriteAverageMsLatency = (long) (diskUsageInfo.Value.WriteAverageSecondsLatency * 1000),
                        CurrentQueueLength = diskUsageInfo.Value.CurrentQueueLength,
                        UtilizedPercent = 100 - diskUsageInfo.Value.IdleTimePercent,
                        ReadsPerSecond = diskUsageInfo.Value.DiskReadsPerSecond,
                        WritesPerSecond = diskUsageInfo.Value.DiskWritesPerSecond,
                        BytesReadPerSecond = diskUsageInfo.Value.BytesReadPerSecond,
                        BytesWrittenPerSecond = diskUsageInfo.Value.BytesWrittenPerSecond
                    };
                    disksUsageInfo[result.DiskName] = result;
                }

                metrics.DisksUsageInfo = disksUsageInfo;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
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

        private class DiskUsage
        {
            public double IdleTimePercent { get; set; }
            public double ReadAverageSecondsLatency { get; set; }
            public double WriteAverageSecondsLatency { get; set; }
            public long DiskReadsPerSecond { get; set; }
            public long DiskWritesPerSecond { get; set; }
            public long BytesReadPerSecond { get; set; }
            public long BytesWrittenPerSecond { get; set; }
            public long CurrentQueueLength { get; set; }
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