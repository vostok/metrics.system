using System;
using System.IO;
using System.Linq;
using Vostok.Metrics.System.Helpers;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Process
{
    internal class NativeMetricsCollector_Linux : IDisposable
    {
        private const string cgroupMemoryLimitFileName = "/sys/fs/cgroup/memory/memory.limit_in_bytes";
        private const string cgroupCpuCfsQuotaFileName = "/sys/fs/cgroup/cpu/cpu.cfs_quota_us";
        private const string cgroupCpuCfsPerioFileName = "/sys/fs/cgroup/cpu/cpu.cfs_period_us";

        private readonly ReusableFileReader systemStatReader = new ReusableFileReader("/proc/stat");
        private readonly ReusableFileReader processStatReader = new ReusableFileReader("/proc/self/stat");
        private readonly ReusableFileReader processStatusReader = new ReusableFileReader("/proc/self/status");
        private readonly ReusableFileReader cgroupMemoryLimitReader = new ReusableFileReader(cgroupMemoryLimitFileName);
        private readonly ReusableFileReader cgroupCpuCfsQuotaReader = new ReusableFileReader(cgroupCpuCfsQuotaFileName);
        private readonly ReusableFileReader cgroupCpuCfsPeriodReader = new ReusableFileReader(cgroupCpuCfsPerioFileName);
        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();

        public void Dispose()
        {
            systemStatReader.Dispose();
            processStatReader.Dispose();
            processStatusReader.Dispose();
        }

        public void Collect(CurrentProcessMetrics metrics)
        {
            var systemStat = ReadSystemStat();
            var processStat = ReadProcessStat();
            var processStatus = ReadProcessStatus();
            var cgroupStatus = ReadCgroupStatus();

            if (processStatus.FileDescriptorsCount.HasValue)
                metrics.HandlesCount = processStatus.FileDescriptorsCount.Value;

            if (processStatus.VirtualMemoryResident.HasValue)
                metrics.MemoryResident = processStatus.VirtualMemoryResident.Value;

            if (processStatus.VirtualMemoryData.HasValue)
                metrics.MemoryPrivate = processStatus.VirtualMemoryData.Value;

            if (systemStat.Filled && processStat.Filled)
            {
                var systemTime = systemStat.SystemTime.Value + systemStat.UserTime.Value + systemStat.IdleTime.Value;
                var processTime = processStat.SystemTime.Value + processStat.UserTime.Value;

                cpuCollector.Collect(metrics, systemTime, processTime, systemStat.CpuCount);
            }

            metrics.CgroupCpuLimitCores = cgroupStatus.CpuLimit;
            metrics.CgroupMemoryLimit = cgroupStatus.MemoryLimit;
        }

        private SystemStat ReadSystemStat()
        {
            var result = new SystemStat();

            try
            {
                if (FileParser.TrySplitLine(systemStatReader.ReadFirstLine(), 5, out var parts) && parts[0] == "cpu")
                {
                    if (ulong.TryParse(parts[1], out var utime))
                        result.UserTime = utime;

                    if (ulong.TryParse(parts[3], out var stime))
                        result.SystemTime = stime;

                    if (ulong.TryParse(parts[4], out var itime))
                        result.IdleTime = itime;
                }

                result.CpuCount = systemStatReader.ReadLines().Count(line => line.StartsWith("cpu")) - 1;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private ProcessStat ReadProcessStat()
        {
            var result = new ProcessStat();

            try
            {
                if (FileParser.TrySplitLine(processStatReader.ReadFirstLine(), 15, out var parts))
                {
                    if (ulong.TryParse(parts[13], out var utime))
                        result.UserTime = utime;

                    if (ulong.TryParse(parts[14], out var stime))
                        result.SystemTime = stime;
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private ProcessStatus ReadProcessStatus()
        {
            var result = new ProcessStatus();

            try
            {
                result.FileDescriptorsCount = Directory.EnumerateFiles("/proc/self/fd/").Count();
            }
            catch (DirectoryNotFoundException)
            {
                // NOTE: Ignored due to process already exited so we don't have to count it's file descriptors count.
                result.FileDescriptorsCount = 0;
            }

            try
            {
                foreach (var line in processStatusReader.ReadLines())
                {
                    if (FileParser.TryParseLong(line, "RssAnon", out var vmRss))
                        result.VirtualMemoryResident = vmRss * 1024L;

                    if (FileParser.TryParseLong(line, "VmData", out var vmData))
                        result.VirtualMemoryData = vmData * 1024L;

                    if (result.Filled)
                        break;
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private ProcessCgroupStatus ReadCgroupStatus()
        {
            var result = new ProcessCgroupStatus();

            if (File.Exists(cgroupMemoryLimitFileName))
            {
                if (long.TryParse(cgroupMemoryLimitReader.ReadFirstLine(), out var memoryLimit))
                    result.MemoryLimit = memoryLimit;
            }

            if (File.Exists(cgroupCpuCfsPerioFileName) && File.Exists(cgroupCpuCfsQuotaFileName))
            {
                if (long.TryParse(cgroupCpuCfsPeriodReader.ReadFirstLine(), out var cpuPeriod)
                    && long.TryParse(cgroupCpuCfsQuotaReader.ReadFirstLine(), out var cpuQuota))
                {
                    result.CpuLimit = (double) cpuQuota / cpuPeriod;
                }
            }

            return result;
        }

        private class SystemStat
        {
            public bool Filled => IdleTime.HasValue && UserTime.HasValue && SystemTime.HasValue;

            public int? CpuCount { get; set; }
            public ulong? IdleTime { get; set; }
            public ulong? UserTime { get; set; }
            public ulong? SystemTime { get; set; }
        }

        private class ProcessStat
        {
            public bool Filled => UserTime.HasValue && SystemTime.HasValue;

            public ulong? UserTime { get; set; }
            public ulong? SystemTime { get; set; }
        }

        private class ProcessStatus
        {
            public bool Filled => FileDescriptorsCount.HasValue && VirtualMemoryResident.HasValue && VirtualMemoryData.HasValue;

            public int? FileDescriptorsCount { get; set; }
            public long? VirtualMemoryResident { get; set; }
            public long? VirtualMemoryData { get; set; }
        }

        private class ProcessCgroupStatus
        {
            public double? CpuLimit { get; set; }
            public long? MemoryLimit { get; set; }
        }
    }
}