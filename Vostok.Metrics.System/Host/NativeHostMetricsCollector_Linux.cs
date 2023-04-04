using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Helpers.Linux;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Linux : IDisposable
    {
        private readonly HostMetricsSettings settings;

        private readonly Regex pidRegex = new Regex("[0-9]+$", RegexOptions.Compiled);

        // See https://man7.org/linux/man-pages/man5/proc.5.html for information about each file
        private readonly ProcStatReader procStatReader;
        private readonly ReusableFileReader memoryReader = new ReusableFileReader("/proc/meminfo");
        private readonly ReusableFileReader vmStatReader = new ReusableFileReader("/proc/vmstat");
        private readonly ReusableFileReader descriptorInfoReader = new ReusableFileReader("/proc/sys/fs/file-nr");

        private readonly HostCpuUtilizationCollector cpuCollector;
        private readonly NetworkUtilizationCollector_Linux networkCollector = new NetworkUtilizationCollector_Linux();
        private readonly DerivativeCollector hardPageFaultCollector = new DerivativeCollector();
        private readonly DiskUsageCollector_Linux diskUsageCollector = new DiskUsageCollector_Linux();

        public NativeHostMetricsCollector_Linux(HostMetricsSettings settings)
        {
            this.settings = settings;
            procStatReader = new ProcStatReader(true); //todo?
            cpuCollector = new HostCpuUtilizationCollector(GetProcessorCount);
        }

        public void Dispose()
        {
            procStatReader.Dispose();
            memoryReader.Dispose();
            vmStatReader.Dispose();
            descriptorInfoReader.Dispose();
            networkCollector.Dispose();
            diskUsageCollector.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            var memInfo = settings.CollectMemoryMetrics ? ReadMemoryInfo() : new MemoryInfo();
            var perfInfo = settings.CollectMiscMetrics ? ReadPerformanceInfo() : new PerformanceInfo();

            if (settings.CollectCpuMetrics)
            {
                ulong usedTime = 0;
                if (procStatReader.TryRead(out var systemStat))
                {
                    usedTime = systemStat.UserTime + systemStat.NicedTime +
                               systemStat.SystemTime + systemStat.IdleTime;
                }
                cpuCollector.Collect(metrics, usedTime, systemStat.IdleTime); //todo  pass cores count here??
            }

            if (memInfo.Filled)
            {
                metrics.MemoryAvailable = memInfo.AvailableMemory.Value;
                metrics.MemoryCached = memInfo.CacheMemory.Value;
                metrics.MemoryKernel = memInfo.KernelMemory.Value;
                metrics.MemoryTotal = memInfo.TotalMemory.Value;
                metrics.MemoryFree = memInfo.FreeMemory.Value;
                metrics.PageFaultsPerSecond = (long) hardPageFaultCollector.Collect(memInfo.MajorPageFaultCount.Value);
            }

            if (perfInfo.Filled)
            {
                metrics.HandleCount = perfInfo.HandleCount.Value;
                metrics.ThreadCount = perfInfo.ThreadCount.Value;
                metrics.ProcessCount = perfInfo.ProcessCount.Value;
            }

            if (settings.CollectNetworkUsageMetrics)
                networkCollector.Collect(metrics);

            if (settings.CollectDiskUsageMetrics)
                diskUsageCollector.Collect(metrics);
        }

        private bool ReadSystemStat(out ProcStat procStat)
        {
            try
            {
                return procStatReader.TryRead(out procStat);
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            procStat = default;
            return false;
        }
        
        private const string libc = "libc.so.6";
            
        [DllImport(libc, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr sysconf(int name);
        const int _SC_NPROCESSORS_ONLN = 84;


        private int? GetProcessorCount()
        {
            //todo super slow, + many garbage
            //auto npc = get_nprocs_conf();
            //auto np = get_nprocs();
            try
            {
                var number = sysconf(_SC_NPROCESSORS_ONLN);
                if ((long)number < 0)
                    return 0;
                return (int)number;
                //return cpuInfoReader.ReadLines().Count(x => x.Contains("processor"));//todo trash!!
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
                return null;
            }
        }

        private MemoryInfo ReadMemoryInfo()
        {
            var result = new MemoryInfo();

            try
            {
                foreach (var line in memoryReader.ReadLines())
                {
                    if (FileParser.TryParseLong(line, "MemTotal", out var memTotal))
                        result.TotalMemory = memTotal * 1024;

                    if (FileParser.TryParseLong(line, "MemAvailable", out var memAvailable))
                        result.AvailableMemory = memAvailable * 1024;

                    if (FileParser.TryParseLong(line, "Cached", out var memCached))
                        result.CacheMemory = memCached * 1024;

                    if (FileParser.TryParseLong(line, "Slab", out var memKernel))
                        result.KernelMemory = memKernel * 1024;

                    if (FileParser.TryParseLong(line, "MemFree", out var memFree))
                        result.FreeMemory = memFree * 1024;
                }

                foreach (var line in vmStatReader.ReadLines())
                {
                    if (FileParser.TryParseLong(line, "pgmajfault", out var hardPageFaultCount))
                        result.MajorPageFaultCount = hardPageFaultCount;
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private PerformanceInfo ReadPerformanceInfo()//todo тоже медленный способ то
        {
            var result = new PerformanceInfo();

            try
            {
                var processDirectories = Directory.EnumerateDirectories("/proc/")
                   .Where(x => pidRegex.IsMatch(x));

                var processCount = 0;
                var threadCount = 0;

                foreach (var processDirectory in processDirectories)
                {
                    try
                    {
                        threadCount += Directory.EnumerateDirectories(Path.Combine(processDirectory, "task")).Count();//todo slow
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // NOTE: Ignored due to process already exited so we don't have to count it's threads and just want to continue.
                        continue;
                    }

                    processCount++;
                }

                result.ProcessCount = processCount;
                result.ThreadCount = threadCount;

                if (FileParser.TrySplitLine(descriptorInfoReader.ReadFirstLine(), 3, out var parts) &&
                    int.TryParse(parts[0], out var allocatedDescriptors) &&
                    int.TryParse(parts[1], out var freeDescriptors))
                    result.HandleCount = allocatedDescriptors - freeDescriptors;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private class PerformanceInfo
        {
            public bool Filled => ProcessCount.HasValue && ThreadCount.HasValue && HandleCount.HasValue;

            public int? ProcessCount { get; set; }
            public int? ThreadCount { get; set; }
            public int? HandleCount { get; set; }
        }

        private class SystemStat
        {
            public bool Filled => UserTime.HasValue && NicedTime.HasValue && SystemTime.HasValue && IdleTime.HasValue;

            public ulong? UserTime { get; set; }
            public ulong? NicedTime { get; set; }
            public ulong? SystemTime { get; set; }
            public ulong? IdleTime { get; set; }
        }

        private class MemoryInfo
        {
            public bool Filled => AvailableMemory.HasValue && KernelMemory.HasValue && CacheMemory.HasValue &&
                                  TotalMemory.HasValue && FreeMemory.HasValue && MajorPageFaultCount.HasValue;

            public long? AvailableMemory { get; set; }
            public long? KernelMemory { get; set; }
            public long? CacheMemory { get; set; }
            public long? FreeMemory { get; set; }
            public long? TotalMemory { get; set; }
            public long? MajorPageFaultCount { get; set; }
        }
    }
}