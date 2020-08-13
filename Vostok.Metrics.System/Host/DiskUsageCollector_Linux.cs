﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskUsageCollector_Linux : IDisposable
    {
        private Dictionary<string, DiskStats> previousDisksStatsInfo = new Dictionary<string, DiskStats>();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly ReusableFileReader diskStatsReader = new ReusableFileReader("/proc/diskstats");

        public void Dispose()
        {
            diskStatsReader?.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            var deltaTime = stopwatch.Elapsed.TotalSeconds;

            var newDisksStatsInfo = new Dictionary<string, DiskStats>();
            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>();

            foreach (var diskStats in ParseDiskstats(diskStatsReader.ReadLines()))
            {
                var result = new DiskUsageInfo {DiskName = diskStats.DiskName};

                if (previousDisksStatsInfo.TryGetValue(diskStats.DiskName, out var previousDiskStats))
                    FillInfo(result, previousDiskStats, diskStats, deltaTime);

                disksUsageInfo[result.DiskName] = result;

                newDisksStatsInfo[diskStats.DiskName] = diskStats;
            }

            metrics.DisksUsageInfo = disksUsageInfo;

            previousDisksStatsInfo = newDisksStatsInfo;

            stopwatch.Restart();
        }

        private void FillInfo(DiskUsageInfo toFill, DiskStats previousDiskStats, DiskStats diskStats, double deltaTime)
        {
            var readsDelta = diskStats.ReadsCount - previousDiskStats.ReadsCount;
            var writesDelta = diskStats.WritesCount - previousDiskStats.WritesCount;
            var timeReadDelta = diskStats.TimeSpentReading - diskStats.TimeSpentReading;
            var timeWriteDelta = diskStats.TimeSpentWriting - diskStats.TimeSpentWriting;

            if (readsDelta > 0)
                toFill.ReadLatency = (double) timeReadDelta / readsDelta;
            if (writesDelta > 0)
                toFill.WriteLatency = (double) timeWriteDelta / writesDelta;

            toFill.CurrentQueueLength = diskStats.CurrentQueueLength;

            toFill.ReadsPerSecond = readsDelta / deltaTime;
            toFill.WritesPerSecond = writesDelta / deltaTime;

            toFill.IdleTimePercent = ((1 - (timeReadDelta + timeWriteDelta) / deltaTime) * 100).Clamp(0, 100);
        }

        private List<DiskStats> ParseDiskstats(IEnumerable<string> diskstats)
        {
            var disksStats = new List<DiskStats>();

            try
            {
                foreach (var line in diskstats)
                {
                    // NOTE: In the most basic form there are 14 parts.
                    // NOTE: See https://www.kernel.org/doc/Documentation/ABI/testing/procfs-diskstats for details.
                    if (FileParser.TrySplitLine(line, 14, out var parts) && IsDiskNumber(parts[1]))
                    {
                        var stats = new DiskStats {DiskName = parts[2]};

                        if (long.TryParse(parts[3], out var readsCount))
                            stats.ReadsCount = readsCount;

                        if (long.TryParse(parts[6], out var timeSpentReading))
                            stats.TimeSpentReading = timeSpentReading;

                        if (long.TryParse(parts[7], out var writesCount))
                            stats.WritesCount = writesCount;

                        if (long.TryParse(parts[10], out var timeSpentWriting))
                            stats.TimeSpentWriting = timeSpentWriting;

                        if (long.TryParse(parts[11], out var currentQueueLength))
                            stats.CurrentQueueLength = currentQueueLength;

                        disksStats.Add(stats);
                    }
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return disksStats;
        }

        private class DiskStats
        {
            public string DiskName { get; set; }
            public long ReadsCount { get; set; }
            public long WritesCount { get; set; }
            public long TimeSpentReading { get; set; }
            public long TimeSpentWriting { get; set; }
            public long CurrentQueueLength { get; set; }
        }

        // NOTE: Every 16th minor device number is associated with whole disk (Not just partition)
        // NOTE: See https://www.kernel.org/doc/html/v4.12/admin-guide/devices.html (SCSI disk devices) for details.
        private bool IsDiskNumber(string number)
        {
            return int.TryParse(number, out var value) && value % 16 == 0;
        }
    }
}