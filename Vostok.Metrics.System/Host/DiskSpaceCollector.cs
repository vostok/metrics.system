using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class DiskSpaceCollector : IDisposable
    {
        private readonly ReusableFileReader mountsReaderLinux;
        private readonly Func<DriveInfo, bool> systemFilter;
        private readonly Func<string, string> nameFormatter;
        private readonly StringComparer diskNameComparer;

        private volatile Dictionary<string, string> mountDiskMap = new Dictionary<string, string>();

        public DiskSpaceCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                systemFilter = FilterDisks_Windows;
                nameFormatter = FormatDiskName_Windows;
                diskNameComparer = StringComparer.OrdinalIgnoreCase;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                systemFilter = FilterDisks_Linux;
                nameFormatter = FormatDiskName_Linux;
                mountsReaderLinux = new ReusableFileReader("/proc/mounts");
                diskNameComparer = StringComparer.Ordinal;
            }
        }

        public void Dispose()
            => mountsReaderLinux?.Dispose();

        public void Collect(HostMetrics metrics)
        {
            var diskSpaceInfos = new Dictionary<string, DiskSpaceInfo>(diskNameComparer);

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

            // NOTE: We are trying to get information from disks which are not ready. This is made for docker compatibility (some of the drives are shown as NotReady for some reason).
            // NOTE: Also, the fact that drive has IsReady set to true doesn't mean much: it can still throw IOExceptions.
            // NOTE: See https://docs.microsoft.com/en-us/dotnet/api/system.io.driveinfo.isready for details.
            //DriveInfo.GetDrives() returns info from /proc/mounts or getmntent call https://man7.org/linux/man-pages/man3/getmntent.3.html
            //systemFilter is useless in current implementation
            foreach (var drive in DriveInfo.GetDrives().Where(x => systemFilter(x) && x.DriveType == DriveType.Fixed))
            {
                var result = new DiskSpaceInfo();

                try
                {
                    result.DiskName = nameFormatter(drive.Name);
                    result.RootDirectory = drive.RootDirectory.FullName;
                    result.FreeBytes = drive.TotalFreeSpace; //todo useless in linux, need AvailableFreeSpace for see what space avail for real usage.
                    result.TotalCapacityBytes = drive.TotalSize;
                    if (result.TotalCapacityBytes != 0)
                        result.FreePercent = result.FreeBytes * 100d / result.TotalCapacityBytes;
                }
                catch (Exception error)
                {
                    if (drive.IsReady)
                        InternalLogger.Warn(error);
                    continue;
                }

                yield return result;
            }
        }

        private void UpdateMountMap()
        {
            //todo bad method, trash devices like "loop0" included
            //use list direcroty "/dev/disk/by-path" and follow symlinks, remove duplicated...
            /*
dgorlov@sd2-k-lin01:~$ ls -alh /dev/disk/by-path
total 0
drwxr-xr-x 2 root root 160 Feb  6 14:14 .
drwxr-xr-x 6 root root 120 Feb  6 14:14 ..
lrwxrwxrwx 1 root root   9 Feb  6 14:14 pci-0000:00:1f.2-ata-1 -> ../../sda
lrwxrwxrwx 1 root root  10 Feb  6 14:15 pci-0000:00:1f.2-ata-1-part1 -> ../../sda1
lrwxrwxrwx 1 root root  10 Feb  6 14:14 pci-0000:00:1f.2-ata-1-part2 -> ../../sda2
lrwxrwxrwx 1 root root   9 Jun 17 14:02 pci-0000:00:1f.2-ata-2 -> ../../sdb
lrwxrwxrwx 1 root root   9 Feb  6 14:14 pci-0000:00:1f.2-ata-3 -> ../../sdc
lrwxrwxrwx 1 root root   9 Feb  6 14:14 pci-0000:00:1f.2-ata-4 -> ../../sdd             
            */
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
                InternalLogger.Warn(error);
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