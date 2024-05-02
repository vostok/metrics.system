using System;
using System.IO;
using System.Linq;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Helpers.Linux;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Process
{
    internal class NativeProcessMetricsCollector_Linux : INativeProcessMetricsCollector_Linux
    {
        private const string cgroupMemoryLimitFileName = "/sys/fs/cgroup/memory/memory.limit_in_bytes";
        private const string cgroupCpuCfsQuotaFileName = "/sys/fs/cgroup/cpu/cpu.cfs_quota_us";
        private const string cgroupCpuCfsPeriodFileName = "/sys/fs/cgroup/cpu/cpu.cfs_period_us";

        private const string cgroupNoMemoryLimitValue = "9223372036854771712";
        private const string cgroupNoCpuLimitValue = "-1";
        private readonly LinuxProcessMetricsSettings settings;

        private readonly ProcStatReader procStatReader;
        private readonly ProcSelfStatReader procSelfStatReader;
        private readonly ProcSelfStatmReader procSelfStatmReader;

        private readonly ReusableFileReader cgroupMemoryLimitReader = new ReusableFileReader(cgroupMemoryLimitFileName);
        private readonly ReusableFileReader cgroupCpuCfsQuotaReader = new ReusableFileReader(cgroupCpuCfsQuotaFileName);
        private readonly ReusableFileReader cgroupCpuCfsPeriodReader = new ReusableFileReader(cgroupCpuCfsPeriodFileName);
        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();
        private readonly CpuCountMeter cpuCountMeter;

        public NativeProcessMetricsCollector_Linux(LinuxProcessMetricsSettings settings)
        {
            this.settings = settings ?? new LinuxProcessMetricsSettings();
            procStatReader = new ProcStatReader();
            procSelfStatmReader = new ProcSelfStatmReader();
            procSelfStatReader = new ProcSelfStatReader();
            cpuCountMeter = new CpuCountMeter(this.settings.UseDotnetCpuCount);
        }

        public void Dispose()
        {
            procStatReader.Dispose();
            procSelfStatReader.Dispose();
            procSelfStatmReader.Dispose();
        }

        public void Collect(CurrentProcessMetrics metrics)
        {
            var cgroupStatus = ReadCgroupStatus();

            if (ReadOpenFilesCount(out var openFilesCount))
                metrics.HandlesCount = openFilesCount;

            if (TryReadProcessStatm(out var statm))
            {
                metrics.MemoryResident = statm.PrivateRss;

                metrics.MemoryPrivate = statm.DataSize;
            }

            if (TryReadProcessStat(out var processStat) && ReadSystemStat(out var systemStat))
            {
                var systemTime = systemStat.GetTotalTime();
                var processTime = processStat.stime + processStat.utime;

                cpuCollector.Collect(metrics, systemTime, processTime, cpuCountMeter.GetCpuCount());
            }

            metrics.CgroupCpuLimitCores = cgroupStatus.CpuLimit;
            metrics.CgroupMemoryLimit = cgroupStatus.MemoryLimit;
        }

        private bool ReadSystemStat(out ProcStat procStat)
        {
            try
            {
                if (procStatReader.TryRead(out procStat))
                    return true;
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }

            procStat = default;
            return false;
        }

        private bool TryReadProcessStat(out ProcSelfStat value)
        {
            try
            {
                if (procSelfStatReader.TryRead(out value))
                    return true;
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }

            value = default;
            return false;
        }

        private bool TryReadProcessStatm(out ProcSelfStatm value)
        {
            try
            {
                if (procSelfStatmReader.TryRead(out value))
                    return true;
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }

            value = default;
            return false;
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

        private struct ProcessCgroupStatus
        {
            public double? CpuLimit { get; set; }
            public long? MemoryLimit { get; set; }
        }
    }
}