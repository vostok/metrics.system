using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host;

internal static class DiskStatsParser
{
    private static readonly Regex NvmeDiskNamePattern = new(@"^nvme\dn\d$", RegexOptions.Compiled);
        
    public static List<DiskStats> Parse(IEnumerable<string> lines)
    {
        var disksStats = new List<DiskStats>();

        try
        {
            foreach (var line in lines)
            {
                // NOTE: In the most basic form there are 14 parts.
                // NOTE: See https://www.kernel.org/doc/Documentation/ABI/testing/procfs-diskstats for details.
                if (!FileParser.TrySplitLine(line, 14, out var parts))
                    continue;
                    
                    
                if ((IsDiskNumber(parts[0]) && IsWholeDiskNumber(parts[1])) || IsNvme(parts[0], parts[2]))
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
            InternalLogger.Warn(error);
        }

        return disksStats;
    }

    private static bool IsNvme(string majorNumber, string name)
    {
        if (!int.TryParse(majorNumber, out var majorId) || majorId != 259)
            return false;

        return NvmeDiskNamePattern.IsMatch(name);
    }
        
    #region Disk Numbers

    private static readonly HashSet<int> DiskNumbers = new HashSet<int> {8, 65, 66, 67, 68, 69, 70, 71, 128, 129, 130, 131, 132, 133, 134, 135, 202};

    // NOTE: Disk numbers are listed here: https://www.kernel.org/doc/html/v4.12/admin-guide/devices.html
    private static bool IsDiskNumber(string number)
    {
        return int.TryParse(number, out var value) && DiskNumbers.Contains(value);
    }

    // NOTE: Every 16th minor device number is associated with a whole disk (Not just partition)
    // NOTE: See https://www.kernel.org/doc/html/v4.12/admin-guide/devices.html (SCSI disk devices) for details.
    private static bool IsWholeDiskNumber(string number)
    {
        return int.TryParse(number, out var value) && value % 16 == 0;
    }

    #endregion
}