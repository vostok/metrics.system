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
            metrics.DiskSpaceInfos = GetDiskSpaceInfos().ToDictionary(x => x.Name);
        }

        private IEnumerable<DiskSpaceInfo> GetDiskSpaceInfos()
        {
            foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
            {
                var result = new DiskSpaceInfo();

                try
                {
                    result.Name = drive.Name.Replace(":\\", string.Empty);
                    result.FreeBytes = drive.TotalFreeSpace;
                    result.TotalCapacityBytes = drive.TotalSize;
                    result.FreePercent = drive.TotalFreeSpace * 100d / drive.TotalSize;
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