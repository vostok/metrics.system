﻿using System;
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
        private readonly ReusableFileReader mountsReaderLinux;
        private readonly Func<DriveInfo, bool> systemFilter;
        private readonly Func<string, string> nameFormatter;

        private volatile Dictionary<string, string> mountDiskMap = new Dictionary<string, string>();

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
                mountsReaderLinux = new ReusableFileReader("/proc/mounts");
            }
        }

        public void Dispose()
            => mountsReaderLinux?.Dispose();

        public void Collect(HostMetrics metrics)
        {
            var diskSpaceInfos = new Dictionary<string, DiskSpaceInfo>();

            foreach (var info in GetDiskSpaceInfos())
            {
                if (!diskSpaceInfos.ContainsKey(info.DiskName))
                    diskSpaceInfos[info.DiskName] = info;
            }

            metrics.DisksSpaceInfo = diskSpaceInfos;
        }

        private IEnumerable<DiskSpaceInfo> GetDiskSpaceInfos()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                UpdateMountMap();

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
                foreach (var mountLine in mountsReaderLinux.ReadLines())
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

        private bool FilterDisks_Linux(DriveInfo disk) => mountDiskMap.ContainsKey(disk.Name);

        private bool FilterDisks_Windows(DriveInfo disk) => true;

        private string FormatDiskName_Windows(string diskName) => diskName.Replace(":\\", string.Empty);

        private string FormatDiskName_Linux(string diskName)
            => mountDiskMap[diskName]
                .Replace("/dev/", string.Empty)
                .Replace("/", "-");
    }
}
