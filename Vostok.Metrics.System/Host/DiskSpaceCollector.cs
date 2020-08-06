using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskSpaceCollector
    {
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
            foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
            {
                var result = new DiskSpaceInfo();

                try
                {
                    result.DiskName = drive.Name.Replace(":\\", string.Empty);
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
    }
}