using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskUsageCollector_Linux
    {
        private Dictionary<string, DiskStats> previousDisksStatsInfo = new Dictionary<string, DiskStats>();
        private readonly Stopwatch stopwatch = new Stopwatch();

        public void Collect(HostMetrics metrics, IEnumerable<string> diskstatsLines)
        {
            var deltaTime = (double) stopwatch.ElapsedTicks / Stopwatch.Frequency;

            var disksUsageInfo = new Dictionary<string, DiskUsageInfo>();

            foreach (var diskStats in ParseDiskstats(diskstatsLines))
            {
                var result = new DiskUsageInfo {DiskName = diskStats.DiskName};

                if (!previousDisksStatsInfo.ContainsKey(diskStats.DiskName))
                    previousDisksStatsInfo[diskStats.DiskName] = diskStats;
                else
                {
                    var previousDiskStats = previousDisksStatsInfo[diskStats.DiskName];

                    var readsDelta = diskStats.ReadsCount - previousDiskStats.ReadsCount;
                    var writesDelta = diskStats.WritesCount - previousDiskStats.WritesCount;
                    var timeReadDelta = diskStats.TimeSpentReading - diskStats.TimeSpentReading;
                    var timeWriteDelta = diskStats.TimeSpentWriting - diskStats.TimeSpentWriting;

                    if (readsDelta > 0)
                        result.ReadLatency = (double) timeReadDelta / readsDelta;
                    if (writesDelta > 0)
                        result.WriteLatency = (double) timeWriteDelta / writesDelta;

                    result.CurrentQueueLength = diskStats.CurrentQueueLength - previousDiskStats.CurrentQueueLength;

                    result.DiskReadsPerSecond = readsDelta / deltaTime;
                    result.DiskWritesPerSecond = writesDelta / deltaTime;

                    result.IdleTimePercent = ((1 - (timeReadDelta + timeWriteDelta) / deltaTime) * 100).Clamp(0, 100);
                }

                disksUsageInfo[result.DiskName] = result;
            }

            metrics.DisksUsageInfo = disksUsageInfo;

            previousDisksStatsInfo = previousDisksStatsInfo
               .Where(x => disksUsageInfo.ContainsKey(x.Key))
               .ToDictionary(x => x.Key, y => y.Value);

            stopwatch.Restart();
        }

        private List<DiskStats> ParseDiskstats(IEnumerable<string> diskstats)
        {
            var disksStats = new List<DiskStats>();

            try
            {
                foreach (var line in diskstats)
                {
                    if (FileParser.TrySplitLine(line, 14, out var parts) && parts[2].Contains("sd") && !char.IsDigit(parts[2].Last()))
                    {
                        var stats = new DiskStats {DiskName = parts[2]};

                        if (long.TryParse(parts[4], out var readsCount))
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
    }
}