using System;
using Vostok.Metrics.System.Helpers;

// ReSharper disable PossibleInvalidOperationException

namespace Vostok.Metrics.System.Process
{
    internal class LinuxNativeMetricsCollector
    {
        private readonly ReusableFileReader systemStatReader = new ReusableFileReader("/proc/stat");
        private readonly ReusableFileReader processStatReader = new ReusableFileReader("/proc/self/stat");
        private readonly ReusableFileReader processStatusReader = new ReusableFileReader("/proc/self/status");
        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();

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

            if (TrySplitLine(systemStatReader.ReadFirstLine(), 5, out var parts) && parts[0] == "cpu")
            {
                if (ulong.TryParse(parts[1], out var utime))
                    result.UserTime = utime;

                if (ulong.TryParse(parts[3], out var stime))
                    result.SystemTime = stime;

                if (ulong.TryParse(parts[4], out var itime))
                    result.IdleTime = itime;
            }

            return result;
        }

        private ProcessStat ReadProcessStat()
        {
            var result = new ProcessStat();

            if (TrySplitLine(processStatReader.ReadFirstLine(), 15, out var parts))
            {
                if (ulong.TryParse(parts[13], out var utime))
                    result.UserTime = utime;

                if (ulong.TryParse(parts[14], out var stime))
                    result.SystemTime = stime;
            }

            return result;
        }

        private ProcessStatus ReadProcessStatus()
        {
            var result = new ProcessStatus();

            bool TryParse(string line, string name, out int value)
            {
                value = 0;

                return line.StartsWith(name) && TrySplitLine(line, 2, out var parts) && int.TryParse(parts[1], out value);
            }

            foreach (var line in processStatusReader.ReadLines())
            {
                if (TryParse(line, "FDSize", out var fdSize))
                    result.FileDescriptorsSize = fdSize;

                if (TryParse(line, "VmRSS", out var vmRss))
                    result.VirtualMemoryResident = vmRss * 1024;

                if (TryParse(line, "VmData", out var vmData))
                    result.VirtualMemoryData = vmData * 1024;

                if (result.Filled)
                    break;
            }

            return result;
        }

        private static bool TrySplitLine(string line, int minParts, out string[] parts)
            => (parts = line?.Split(null as char[], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()).Length >= minParts;

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
