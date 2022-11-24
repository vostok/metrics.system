using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;
using Vostok.Sys.Metrics.PerfCounters;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class DiskUsageCollector_Windows : IDiskUsageCollector
    {
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
            .AddCounter("LogicalDisk", "Disk Reads/sec", (context, value) => context.Result.DiskReadsPerSecond = (long)value)
            .AddCounter("LogicalDisk", "Disk Writes/sec", (context, value) => context.Result.DiskWritesPerSecond = (long)value)
            .AddCounter(
                "LogicalDisk",
                "Current Disk Queue Length",
                (context, value) => context.Result.CurrentQueueLength = (long)value)
            .AddCounter("LogicalDisk", "Disk Read Bytes/sec", (context, value) => context.Result.BytesReadPerSecond = (long)value)
            .AddCounter("LogicalDisk", "Disk Write Bytes/sec", (context, value) => context.Result.BytesWrittenPerSecond = (long)value)
            .BuildForMultipleInstances("*:");

        public Dictionary<string, DiskUsageInfo> Collect()
        {
            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>();

            try
            {
                foreach (var diskUsageInfo in diskUsageCounter.Observe())
                {
                    var result = new DiskUsageInfo
                    {
                        DiskName = diskUsageInfo.Instance.Replace(":", string.Empty),
                        ReadAverageMsLatency = (long)(diskUsageInfo.Value.ReadAverageSecondsLatency * 1000),
                        WriteAverageMsLatency = (long)(diskUsageInfo.Value.WriteAverageSecondsLatency * 1000),
                        CurrentQueueLength = diskUsageInfo.Value.CurrentQueueLength,
                        UtilizedPercent = 100 - diskUsageInfo.Value.IdleTimePercent,
                        ReadsPerSecond = diskUsageInfo.Value.DiskReadsPerSecond,
                        WritesPerSecond = diskUsageInfo.Value.DiskWritesPerSecond,
                        BytesReadPerSecond = diskUsageInfo.Value.BytesReadPerSecond,
                        BytesWrittenPerSecond = diskUsageInfo.Value.BytesWrittenPerSecond
                    };
                    disksUsageInfo[result.DiskName] = result;
                }

                return disksUsageInfo;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
                return null;
            }
        }

        public void Dispose()
        {
            diskUsageCounter.Dispose();
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
    }
}