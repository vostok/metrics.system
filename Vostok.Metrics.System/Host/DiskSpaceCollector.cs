using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

// ReSharper disable InconsistentNaming

namespace Vostok.Metrics.System.Host
{
    internal class DiskSpaceCollector : IDisposable
    {
        private readonly ReusableFileReader mountsReader_Linux;
        private Dictionary<string, string> mountDiskMap = new Dictionary<string, string>();
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
                mountsReader_Linux = new ReusableFileReader("/proc/mounts");
            }
        }

        public void Collect(HostMetrics metrics)
        {
            var diskSpaceInfos = new Dictionary<string, DiskSpaceInfo>();

            foreach (var info in GetDiskSpaceInfos())
            {
                if (!diskSpaceInfos.ContainsKey(info.DiskName))
                    diskSpaceInfos[info.DiskName] = info;
                else
                    InternalErrorLogger.Warn(new Exception($"Disk with the same name has already been added. DiskName: {info.DiskName}."));
            }

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

        private void UpdateMountMap()
        {
            mountDiskMap = new Dictionary<string, string>();

            try
            {
                foreach (var mountLine in mountsReader_Linux.ReadLines())
                {
                    if (FileParser.TrySplitLine(mountLine, 2, out var parts) && parts[0].Contains("/dev/"))
                        mountDiskMap[parts[1]] = parts[0];
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private bool FilterDisks_Linux(DriveInfo disk)
        {
            UpdateMountMap();
            return mountDiskMap.ContainsKey(disk.Name);
        }

        private bool FilterDisks_Windows(DriveInfo disk) => true;

        private string FormatDiskName_Windows(string diskName) => diskName.Replace(":\\", string.Empty);

        private string FormatDiskName_Linux(string diskName) => mountDiskMap[diskName].Replace("/dev/", string.Empty);

        public void Dispose()
        {
            mountsReader_Linux?.Dispose();
        }
    }
}