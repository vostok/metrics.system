using System;
using Vostok.Metrics.System.Helpers;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Process
{
    internal class NativeMetricsCollector_Linux : IDisposable
    {
        private readonly ReusableFileReader systemStatReader = new ReusableFileReader("/proc/stat");
        private readonly ReusableFileReader processStatReader = new ReusableFileReader("/proc/self/stat");
        private readonly ReusableFileReader processStatusReader = new ReusableFileReader("/proc/self/status");
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

            if (processStatus.FileDescriptorsSize.HasValue)
                metrics.HandlesCount = processStatus.FileDescriptorsSize.Value;

            if (processStatus.VirtualMemoryResident.HasValue)
                metrics.MemoryResident = processStatus.VirtualMemoryResident.Value;

            if (processStatus.VirtualMemoryData.HasValue)
                metrics.MemoryPrivate = processStatus.VirtualMemoryData.Value;

            if (systemStat.Filled && processStat.Filled)
            {
                var systemTime = systemStat.SystemTime.Value + systemStat.UserTime.Value + systemStat.IdleTime.Value;
                var processTime = processStat.SystemTime.Value + processStat.UserTime.Value;

                cpuCollector.Collect(metrics, systemTime, processTime);
            }
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
                foreach (var line in processStatusReader.ReadLines())
                {
                    if (FileParser.TryParse(line, "FDSize", out var fdSize))
                        result.FileDescriptorsSize = (int) fdSize;

                    if (FileParser.TryParse(line, "VmRSS", out var vmRss))
                        result.VirtualMemoryResident = vmRss * 1024L;

                    if (FileParser.TryParse(line, "VmData", out var vmData))
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

        private class SystemStat
        {
            public bool Filled => IdleTime.HasValue && UserTime.HasValue && SystemTime.HasValue;

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
            public bool Filled => FileDescriptorsSize.HasValue && VirtualMemoryResident.HasValue && VirtualMemoryData.HasValue;

            public int? FileDescriptorsSize { get; set; }
            public long? VirtualMemoryResident { get; set; }
            public long? VirtualMemoryData { get; set; }
        }
    }
}