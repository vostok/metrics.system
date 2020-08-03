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
        private readonly Func<IEnumerable<DriveInfo>, IEnumerable<DriveInfo>> additionalDiskFilter;
        private readonly Func<string, string> nameFormatter;

        public DiskSpaceCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                additionalDiskFilter = FilterDisks_Windows;
                nameFormatter = FormatDiskName_Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                additionalDiskFilter = FilterDisks_Linux;
                nameFormatter = FormatDiskName_Linux;
            }
        }

        public void Collect(HostMetrics metrics)
        {
            var diskSpaceInfos = new Dictionary<string, DiskSpaceInfo>();

            foreach (var info in GetDiskSpaceInfos())
            {
                if (!diskSpaceInfos.ContainsKey(info.Name))
                    diskSpaceInfos[info.Name] = info;
            }

            metrics.DiskSpaceInfos = diskSpaceInfos;
        }

        private IEnumerable<DiskSpaceInfo> GetDiskSpaceInfos()
        {
            foreach (var drive in additionalDiskFilter(DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed)))
            {
                var result = new DiskSpaceInfo();

                try
                {
                    result.Name = nameFormatter(drive.Name);
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

        private IEnumerable<DriveInfo> FilterDisks_Linux(IEnumerable<DriveInfo> disks) => disks.Where(x => x.Name.Contains("/dev/sd"));

        private IEnumerable<DriveInfo> FilterDisks_Windows(IEnumerable<DriveInfo> disks) => disks;

        private string FormatDiskName_Windows(string diskName) => diskName.Replace(":\\", string.Empty);

        private string FormatDiskName_Linux(string diskName) => diskName.Replace("/dev/", string.Empty);
    }
}