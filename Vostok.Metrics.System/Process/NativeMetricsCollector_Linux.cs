using System;
using System.IO;
using System.Linq;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Helpers.Linux;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Process
{

    internal class NativeMetricsCollector_Linux : IDisposable
    {
        private const string cgroupMemoryLimitFileName = "/sys/fs/cgroup/memory/memory.limit_in_bytes";
        private const string cgroupCpuCfsQuotaFileName = "/sys/fs/cgroup/cpu/cpu.cfs_quota_us";
        private const string cgroupCpuCfsPeriodFileName = "/sys/fs/cgroup/cpu/cpu.cfs_period_us";

        private const string cgroupNoMemoryLimitValue = "9223372036854771712";
        private const string cgroupNoCpuLimitValue = "-1";
        private readonly LinuxProcessMetricsSettings settings;

        private readonly ProcStatReader procStatReader;
        private readonly ReusableFileReader processStatReader = new ReusableFileReader("/proc/self/stat"); //300b  //cpu times
        private readonly ProcSelfStatmReader procSelfStatmReader;

        private readonly ReusableFileReader cgroupMemoryLimitReader = new ReusableFileReader(cgroupMemoryLimitFileName);
        private readonly ReusableFileReader cgroupCpuCfsQuotaReader = new ReusableFileReader(cgroupCpuCfsQuotaFileName);
        private readonly ReusableFileReader cgroupCpuCfsPeriodReader = new ReusableFileReader(cgroupCpuCfsPeriodFileName);
        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();

        public NativeMetricsCollector_Linux(LinuxProcessMetricsSettings settings)
        {
            this.settings = settings ?? new LinuxProcessMetricsSettings();
            procStatReader = new ProcStatReader(this.settings.UseDotnetCpuCount);
            procSelfStatmReader = new ProcSelfStatmReader();
        }

        public void Dispose()
        {
            procStatReader.Dispose();
            processStatReader.Dispose();
            procSelfStatmReader.Dispose();
        }

        public void Collect(CurrentProcessMetrics metrics)
        {
            var processStat = ReadProcessStat();

            var cgroupStatus = ReadCgroupStatus();

            if (ReadOpenFilesCount(out var openFilesCount))
                metrics.HandlesCount = openFilesCount;

            if (procSelfStatmReader.TryRead(out var statm))
            {
                metrics.MemoryResident = statm.PrivateRss;

                metrics.MemoryPrivate = statm.DataSize;
            }

            if (processStat.Filled && ReadSystemStat(out var systemStat))
            {
                //todo nice time??
                var systemTime = systemStat.SystemTime + systemStat.UserTime + systemStat.IdleTime; //тут вроде как прошедшее системное время... *cores
                var processTime = processStat.SystemTime.Value + processStat.UserTime.Value;

                //todo тут кажется не то передают, в systemTime не все время учтено...
                //todo IdleTime учитывает ядра??
                cpuCollector.Collect(metrics, systemTime, processTime, systemStat.CpuCount);
            }

            metrics.CgroupCpuLimitCores = cgroupStatus.CpuLimit;
            metrics.CgroupMemoryLimit = cgroupStatus.MemoryLimit;
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

        private bool ReadOpenFilesCount(out int filesCount)
        {
            filesCount = 0;

            if (!settings.DisableOpenFilesCount)
            {
                try
                {
                    //https://unix.stackexchange.com/questions/365922/monitoring-number-of-open-fds-per-process-efficiently
                    //кажется быстро не посчитать
                    // /proc/PID/status FDSize не то
                    filesCount = Directory.EnumerateFiles("/proc/self/fd/").Count();
                    return true;
                }
                catch (DirectoryNotFoundException)
                {
                    // NOTE: Ignored due to process already exited so we don't have to count it's file descriptors count.
                }
            }

        

            return false;
        }

        // See https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/6/html/resource_management_guide/sec-cpu
        // and https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/6/html/resource_management_guide/sec-memory
        // for details
        private ProcessCgroupStatus ReadCgroupStatus()
        {
            var result = new ProcessCgroupStatus();
            if (!settings.DisableCgroupStats)
            {
                if (cgroupMemoryLimitReader.TryReadFirstLine(out var memoryLimitLine)
                    && memoryLimitLine != cgroupNoMemoryLimitValue
                    && long.TryParse(memoryLimitLine, out var memoryLimit))
                {
                    result.MemoryLimit = memoryLimit;
                }

                if (cgroupCpuCfsPeriodReader.TryReadFirstLine(out var cpuPeriodLine)
                    && cgroupCpuCfsQuotaReader.TryReadFirstLine(out var cpuQuotaLine)
                    && cpuQuotaLine != cgroupNoCpuLimitValue
                    && long.TryParse(cpuPeriodLine, out var cpuPeriod)
                    && long.TryParse(cpuQuotaLine, out var cpuQuota))
                {
                    result.CpuLimit = (double)cpuQuota / cpuPeriod;
                }
            }

            return result;
        }

        private class ProcessStat
        {
            public bool Filled => UserTime.HasValue && SystemTime.HasValue;

            public ulong? UserTime { get; set; }
            public ulong? SystemTime { get; set; }
        }

        private struct ProcessCgroupStatus
        {
            public double? CpuLimit { get; set; }
            public long? MemoryLimit { get; set; }
        }
    }
}