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

        public void Collect(HostMetrics metrics) //todo не мусорить строками
        {
            var deltaSeconds = stopwatch.Elapsed.TotalSeconds;

            var newDisksStatsInfo = new Dictionary<string, DiskStats>();
            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>(); //case sensitive in linux

            foreach (var diskStats in DiskStatsParser.Parse(diskStatsReader.ReadLines()))
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

            if (deltaSeconds > 0d)
            {
                toFill.ReadsPerSecond = (long)(readsDelta / deltaSeconds);
                toFill.WritesPerSecond = (long)(writesDelta / deltaSeconds);

                // NOTE: Every sector equals 512B, so it's easy to convert these values.
                // NOTE: See https://www.man7.org/linux/man-pages/man1/iostat.1.html for details about sector size.
                toFill.BytesReadPerSecond = (long)(sectorsReadDelta * 512d / deltaSeconds);
                toFill.BytesWrittenPerSecond = (long)(sectorsWrittenDelta * 512d / deltaSeconds);

                toFill.UtilizedPercent = (100d * msSpentDoingIoDelta / (deltaSeconds * 1000)).Clamp(0, 100);
            }
        }
    }
}