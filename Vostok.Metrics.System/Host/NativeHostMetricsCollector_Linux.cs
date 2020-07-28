using System;
using Vostok.Metrics.System.Helpers;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Host
{
    public class NativeHostMetricsCollector_Linux : IDisposable
    {
        private readonly ReusableFileReader systemStatReader = new ReusableFileReader("/proc/stat");
        private readonly ReusableFileReader memoryReader = new ReusableFileReader("/proc/meminfo");
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector();

        public void Dispose()
        {
            systemStatReader.Dispose();
            memoryReader.Dispose();
        }

        //https://supportcenter.checkpoint.com/supportcenter/portal?eventSubmit_doGoviewsolutiondetails=&solutionid=sk65143
        public void Collect(HostMetrics metrics)
        {
            var systemStat = ReadSystemStat();
            var memInfo = ReadMemoryInfo();

            if (systemStat.Filled)
            {
                var systemTime = systemStat.UserTime.Value + systemStat.NicedTime.Value + systemStat.SystemTime.Value +
                                 systemStat.IOWaitTime.Value + systemStat.InterruptsTime.Value +
                                 systemStat.SoftInterruptsTime.Value;

                cpuCollector.Collect(metrics, systemTime, systemStat.IdleTime.Value);
            }

            if (memInfo.Filled)
            {
                metrics.MemoryAvailable = memInfo.AvailableMemory.Value;
                metrics.MemoryCached = memInfo.CacheMemory.Value;
                metrics.MemoryKernel = memInfo.KernelMemory.Value;
                metrics.MemoryTotal = memInfo.TotalMemory.Value;
            }
        }

        private SystemStat ReadSystemStat()
        {
            var result = new SystemStat();

            try
            {
                if (FileParser.TrySplitLine(systemStatReader.ReadFirstLine(), 7, out var parts) && parts[0] == "cpu")
                {
                    if (ulong.TryParse(parts[1], out var utime))
                        result.UserTime = utime;

                    if (ulong.TryParse(parts[2], out var ntime))
                        result.NicedTime = ntime;

                    if (ulong.TryParse(parts[3], out var stime))
                        result.SystemTime = stime;

                    if (ulong.TryParse(parts[4], out var itime))
                        result.IdleTime = itime;

                    if (ulong.TryParse(parts[5], out var iotime))
                        result.IOWaitTime = iotime;

                    if (ulong.TryParse(parts[6], out var intime))
                        result.InterruptsTime = intime;

                    if (ulong.TryParse(parts[7], out var sintime))
                        result.SoftInterruptsTime = sintime;
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
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
                        result.AvailableMemory = memAvailable;

                    if (FileParser.TryParseLong(line, "Cached", out var memCached))
                        result.CacheMemory = memCached;

                    if (FileParser.TryParseLong(line, "Slab", out var memKernel))
                        result.KernelMemory = memKernel;
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return result;
        }

        private class SystemStat
        {
            public bool Filled => UserTime.HasValue && NicedTime.HasValue && SystemTime.HasValue &&
                                  IdleTime.HasValue && IOWaitTime.HasValue && InterruptsTime.HasValue &&
                                  SoftInterruptsTime.HasValue;

            public ulong? UserTime { get; set; }
            public ulong? NicedTime { get; set; }
            public ulong? SystemTime { get; set; }
            public ulong? IdleTime { get; set; }
            public ulong? IOWaitTime { get; set; }
            public ulong? InterruptsTime { get; set; }
            public ulong? SoftInterruptsTime { get; set; }
        }

        private class MemoryInfo
        {
            public bool Filled => AvailableMemory.HasValue && KernelMemory.HasValue && CacheMemory.HasValue && TotalMemory.HasValue;

            public long? AvailableMemory { get; set; }
            public long? KernelMemory { get; set; }
            public long? CacheMemory { get; set; }
            public long? TotalMemory { get; set; }
        }
    }
}