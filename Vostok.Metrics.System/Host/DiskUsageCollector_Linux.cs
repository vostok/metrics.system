using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskUsageCollector_Linux : IDisposable
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly ReusableFileReader diskStatsReader = new ReusableFileReader("/proc/diskstats");
        private volatile Dictionary<string, DiskStats> previousDisksStatsInfo = new Dictionary<string, DiskStats>();

        public void Dispose()
        {
            diskStatsReader?.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            var deltaSeconds = stopwatch.Elapsed.TotalSeconds;

            var newDisksStatsInfo = new Dictionary<string, DiskStats>();
            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>();

            foreach (var diskStats in ParseDiskstats(diskStatsReader.ReadLines()))
            {
                var result = new DiskUsageInfo {DiskName = diskStats.DiskName};

                if (previousDisksStatsInfo.TryGetValue(diskStats.DiskName, out var previousDiskStats))
                    FillInfo(result, previousDiskStats, diskStats, deltaSeconds);

                disksUsageInfo[result.DiskName] = result;

                newDisksStatsInfo[diskStats.DiskName] = diskStats;
            }

            metrics.DisksUsageInfo = disksUsageInfo;

            previousDisksStatsInfo = newDisksStatsInfo;

            stopwatch.Restart();
        }

        private void FillInfo(DiskUsageInfo toFill, DiskStats previousDiskStats, DiskStats diskStats, double deltaSeconds)
        {
            var readsDelta = diskStats.ReadsCount - previousDiskStats.ReadsCount;
            var writesDelta = diskStats.WritesCount - previousDiskStats.WritesCount;
            var sectorsReadDelta = diskStats.SectorsReadCount - previousDiskStats.SectorsReadCount;
            var sectorsWrittenDelta = diskStats.SectorsWrittenCount - previousDiskStats.SectorsWrittenCount;
            var msReadDelta = diskStats.MsSpentReading - previousDiskStats.MsSpentReading;
            var msWriteDelta = diskStats.MsSpentWriting - previousDiskStats.MsSpentWriting;
            var msSpentDoingIoDelta = diskStats.MsSpentDoingIo - previousDiskStats.MsSpentDoingIo;

            if (readsDelta > 0)
                toFill.ReadAverageMsLatency = (long) ((double) msReadDelta / readsDelta);
            if (writesDelta > 0)
                toFill.WriteAverageMsLatency = (long) ((double) msWriteDelta / writesDelta);

            toFill.CurrentQueueLength = diskStats.CurrentQueueLength;

            toFill.ReadsPerSecond = (long) (readsDelta / deltaSeconds);
            toFill.WritesPerSecond = (long) (writesDelta / deltaSeconds);

            // NOTE: Every sector equals 512B, so it's easy to convert this values.
            // NOTE: See https://www.man7.org/linux/man-pages/man1/iostat.1.html for details about sector size.
            toFill.BytesReadPerSecond = sectorsReadDelta * 512;
            toFill.BytesWrittenPerSecond = sectorsWrittenDelta * 512;

            toFill.UtilizedPercent = (100d * msSpentDoingIoDelta / (deltaSeconds * 1000)).Clamp(0, 100);
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
                    if (FileParser.TrySplitLine(line, 14, out var parts) && IsDiskNumber(parts[0]) && IsWholeDiskNumber(parts[1]))
                    {
                        var stats = new DiskStats {DiskName = parts[2]};

                        if (long.TryParse(parts[3], out var readsCount))
                            stats.ReadsCount = readsCount;

                        if (long.TryParse(parts[5], out var sectorsRead))
                            stats.SectorsReadCount = sectorsRead;

                        if (long.TryParse(parts[6], out var msSpentReading))
                            stats.MsSpentReading = msSpentReading;

                        if (long.TryParse(parts[7], out var writesCount))
                            stats.WritesCount = writesCount;

                        if (long.TryParse(parts[9], out var sectorsWritten))
                            stats.SectorsWrittenCount = sectorsWritten;

                        if (long.TryParse(parts[10], out var msSpentWriting))
                            stats.MsSpentWriting = msSpentWriting;

                        if (long.TryParse(parts[11], out var currentQueueLength))
                            stats.CurrentQueueLength = currentQueueLength;

                        if (long.TryParse(parts[12], out var msSpentDoingIo))
                            stats.MsSpentDoingIo = msSpentDoingIo;

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
            public long SectorsReadCount { get; set; }
            public long SectorsWrittenCount { get; set; }
            public long WritesCount { get; set; }
            public long MsSpentReading { get; set; }
            public long MsSpentWriting { get; set; }
            public long CurrentQueueLength { get; set; }
            public long MsSpentDoingIo { get; set; }
        }

        #region Disk Numbers

        private readonly HashSet<int> diskNumbers = new HashSet<int> {8, 65, 66, 67, 68, 69, 70, 71, 128, 129, 130, 131, 132, 133, 134, 135};

        // NOTE: Disk numbers are listed here: https://www.kernel.org/doc/html/v4.12/admin-guide/devices.html
        private bool IsDiskNumber(string number)
        {
            return int.TryParse(number, out var value) && diskNumbers.Contains(value);
        }

        // NOTE: Every 16th minor device number is associated with a whole disk (Not just partition)
        // NOTE: See https://www.kernel.org/doc/html/v4.12/admin-guide/devices.html (SCSI disk devices) for details.
        private bool IsWholeDiskNumber(string number)
        {
            return int.TryParse(number, out var value) && value % 16 == 0;
        }

        #endregion
    }
}