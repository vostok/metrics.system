using System.IO;
using System.Linq;

namespace Vostok.Metrics.System.Host
{
    internal class DiskSpaceCollector
    {
        public void Collect(HostMetrics metrics)
        {
            metrics.DiskSpaceInfos = DriveInfo.GetDrives()
                .Select(
                    x => new DiskSpaceInfo
                    {
                        Name = x.Name,
                        FreeBytes = x.TotalFreeSpace,
                        TotalCapacityBytes = x.TotalSize,
                        FreePercent = x.TotalFreeSpace * 100d / x.TotalSize
                    })
                .ToDictionary(x => x.Name);
        }
    }
}