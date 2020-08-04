using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskSpaceCollector
    {
        private readonly Func<DriveInfo, bool> systemFilter;
        private readonly Func<string, string> nameFormatter;

        public DiskSpaceCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                systemFilter = FilterDisks_Windows;
                nameFormatter = FormatDiskName_Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                systemFilter = FilterDisks_Linux;
                nameFormatter = FormatDiskName_Linux;
            }
        }

        public void Collect(HostMetrics metrics)
        {
            var diskSpaceInfos = new Dictionary<string, DiskSpaceInfo>();

            foreach (var info in GetDiskSpaceInfos())
                if (!diskSpaceInfos.ContainsKey(info.DiskName))
                    diskSpaceInfos[info.DiskName] = info;

            metrics.DiskSpaceInfos = diskSpaceInfos;
        }

        private IEnumerable<DiskSpaceInfo> GetDiskSpaceInfos()
        {
            foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed && systemFilter(x)))
            {
                var result = new DiskSpaceInfo();

                try
                {
                    result.DiskName = nameFormatter(drive.Name);
                    result.FreeBytes = drive.TotalFreeSpace;
                    result.TotalCapacityBytes = drive.TotalSize;
                    if (result.TotalCapacityBytes != 0)
                        result.FreePercent = result.FreeBytes * 100d / result.TotalCapacityBytes;
                }
                catch (Exception error)
                {
                    InternalErrorLogger.Warn(error);
                    continue;
                }

                yield return result;
            }
        }

        private bool FilterDisks_Linux(DriveInfo disk) => disk.Name.Contains("/dev/sd");

        private bool FilterDisks_Windows(DriveInfo disk) => true;

        private string FormatDiskName_Windows(string diskName) => diskName.Replace(":\\", string.Empty);

        private string FormatDiskName_Linux(string diskName) => diskName.Replace("/dev/", string.Empty);
    }
}