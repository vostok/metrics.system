using System;
using System.IO;
using System.Linq;
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
        private readonly ProcMemInfoReader memoryReader = new ProcMemInfoReader();
        private readonly ProcVmStatReader vmStatReader = new ProcVmStatReader();
        private readonly ReusableFileReader descriptorInfoReader = new ReusableFileReader("/proc/sys/fs/file-nr");

        private readonly HostCpuUtilizationCollector cpuCollector;
        private readonly NetworkUtilizationCollector_Linux networkCollector = new NetworkUtilizationCollector_Linux();
        private readonly DerivativeCollector hardPageFaultCollector = new DerivativeCollector();
        private readonly DiskUsageCollector_Linux diskUsageCollector = new DiskUsageCollector_Linux();
        private readonly CpuCountMeter cpuCountMeter;

        public NativeHostMetricsCollector_Linux(HostMetricsSettings settings)
        {
            this.settings = settings;
            procStatReader = new ProcStatReader();
            cpuCollector = new HostCpuUtilizationCollector();
            cpuCountMeter = new CpuCountMeter(false); //false because we can get system cpu count, not fake count for current process
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
            var perfInfo = settings.CollectMiscMetrics ? ReadPerformanceInfo() : new PerformanceInfo();

            if (settings.CollectCpuMetrics)
            {
                ulong totalTime = 0;
                ulong kernelTime = 0;
                if (ReadSystemStat(out var systemStat))
                {
                    totalTime = systemStat.GetTotalTime();
                    kernelTime = systemStat.SystemTime;
                }

                cpuCollector.Collect(metrics, totalTime, systemStat.IdleTime, kernelTime, cpuCountMeter.GetCpuCount());
            }

            if (settings.CollectMemoryMetrics && TryReadMemoryInfo(out var memInfo, out var vmStat))
            {
                metrics.MemoryAvailable = memInfo.AvailableMemory;
                metrics.MemoryCached = memInfo.CacheMemory;
                metrics.MemoryKernel = memInfo.KernelMemory;
                metrics.MemoryTotal = memInfo.TotalMemory;
                metrics.MemoryFree = memInfo.FreeMemory;
                metrics.PageFaultsPerSecond = (long)hardPageFaultCollector.Collect(vmStat.pgmajfault);
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

        private bool TryReadMemoryInfo(out ProcMemInfo memInfo, out ProcVmStat vmStat)
        {
            vmStat = default;
            try
            {
                return memoryReader.TryRead(out memInfo) && vmStatReader.TryRead(out vmStat);
            }

            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
                memInfo = default;
                return false;
            }
        }

        private PerformanceInfo ReadPerformanceInfo()
        {
            var result = new PerformanceInfo();

            try
            {
                //todo не мусорить хотя бы строками
                //todo 2: Directory.EnumerateDirectories дорогая изза вызовов lstat ненужных. можно напрямую вызывать getdents64 or readdir
                var processDirectories = Directory.EnumerateDirectories("/proc/")
                    .Where(x => pidRegex.IsMatch(x));

                var processCount = 0;
                var threadCount = 0;

                foreach (var processDirectory in processDirectories)
                {
                    try
                    {
                        //note: can read:  /proc/pid/status field Threads:  N
                        threadCount += Directory.EnumerateDirectories(Path.Combine(processDirectory, "task")).Count();
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
    }
}