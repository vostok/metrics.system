using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Local

namespace Vostok.Metrics.System.Process
{
    internal class WindowsNativeMetricsCollector
    {
        private static readonly IntPtr CurrentProcessHandle = GetCurrentProcess();

        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();

        public void Collect(CurrentProcessMetrics metrics)
        {
            CollectMemoryMetrics(metrics);
            CollectHandlesCount(metrics);
            CollectCpuUtilization(metrics);
        }

        private static unsafe void CollectMemoryMetrics(CurrentProcessMetrics metrics)
        {
            if (!GetProcessMemoryInfo(CurrentProcessHandle, out var memoryCounters, sizeof(PROCESS_MEMORY_COUNTERS_EX)))
                ThrowOnError();

            metrics.MemoryResident = (long)memoryCounters.WorkingSetSize;
            metrics.MemoryPrivate = (long)memoryCounters.PrivateUsage;
        }

        private static void CollectHandlesCount(CurrentProcessMetrics metrics)
        {
            if (!GetProcessHandleCount(CurrentProcessHandle, out var handleCount))
                ThrowOnError();

            metrics.HandlesCount = (int)handleCount;
        }

        private void CollectCpuUtilization(CurrentProcessMetrics metrics)
        {
            if (!GetSystemTimes(out _, out var systemKernel, out var systemUser))
                ThrowOnError();

            if (!GetProcessTimes(CurrentProcessHandle, out _, out _, out var processKernel, out var processUser))
                ThrowOnError();

            var systemTime = systemKernel.ToUInt64() + systemUser.ToUInt64();
            var processTime = processKernel.ToUInt64() + processUser.ToUInt64();

            cpuCollector.Collect(metrics, systemTime, processTime);
        }

        private static void ThrowOnError()
        {
            var exception = new Win32Exception();

            throw new Win32Exception(exception.ErrorCode, exception.Message + $"; Error code = {exception.ErrorCode} (0x{exception.ErrorCode:X}).");
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool GetProcessHandleCount(IntPtr hProcess, out uint dwHandleCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessTimes(
            IntPtr hProcess,
            out FILETIME creationTime,
            out FILETIME exitTime,
            out FILETIME kernelTime,
            out FILETIME userTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out FILETIME idleTime,
            out FILETIME kernelTime,
            out FILETIME userTime);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(
            [In] IntPtr process,
            [Out] out PROCESS_MEMORY_COUNTERS_EX ppsmemCounters,
            [In] int cb);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_MEMORY_COUNTERS_EX
        {
            public int cb;
            public int PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
            public UIntPtr PrivateUsage;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FILETIME
        {
            /// <summary>Specifies the low 32 bits of the <see langword="FILETIME" />.</summary>
            public uint dwLowDateTime;

            /// <summary>Specifies the high 32 bits of the <see langword="FILETIME" />.</summary>
            public uint dwHighDateTime;

            public ulong ToUInt64()
            {
                var high = (ulong) dwHighDateTime;
                var low = (ulong) dwLowDateTime;
                return (high << 32) | low;
            }
        }
    }
}
