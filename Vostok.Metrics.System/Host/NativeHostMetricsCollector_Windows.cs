using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class NativeHostMetricsCollector_Windows
    {
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector();

        public void Collect(HostMetrics metrics)
        {
            CollectCpuUtilization(metrics);
            CollectMemoryMetrics(metrics);
        }

        private static void ThrowOnError()
        {
            var exception = new Win32Exception();

            throw new Win32Exception(exception.ErrorCode, exception.Message + $" Error code = {exception.ErrorCode} (0x{exception.ErrorCode:X}).");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out FILETIME idleTime,
            out FILETIME kernelTime,
            out FILETIME userTime);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetPerformanceInfo(
            [Out] out PERFORMANCE_INFORMATION ppsmemCounters,
            [In] int cb);

        private void CollectCpuUtilization(HostMetrics metrics)
        {
            try
            {
                if (!GetSystemTimes(out var idleTime, out var systemKernel, out var systemUser))
                    ThrowOnError();

                var systemTime = systemKernel.ToUInt64() + systemUser.ToUInt64();

                cpuCollector.Collect(metrics, systemTime, idleTime.ToUInt64());
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        private unsafe void CollectMemoryMetrics(HostMetrics metrics)
        {
            try
            {
                if (!GetPerformanceInfo(out var perfInfo, sizeof(PERFORMANCE_INFORMATION)))
                    ThrowOnError();

                metrics.MemoryAvailable = (long) perfInfo.PhysicalAvailable * (long) perfInfo.PageSize;
                metrics.MemoryCached = (long) perfInfo.SystemCache * (long) perfInfo.PageSize;
                metrics.MemoryKernel = (long) perfInfo.KernelTotal * (long) perfInfo.PageSize;
                metrics.MemoryTotal = (long) perfInfo.PhysicalTotal * (long) perfInfo.PageSize;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PERFORMANCE_INFORMATION
        {
            public readonly int cb;
            public readonly UIntPtr CommitTotal;
            public readonly UIntPtr CommitLimit;
            public readonly UIntPtr CommitPeak;
            public readonly UIntPtr PhysicalTotal;
            public readonly UIntPtr PhysicalAvailable;
            public readonly UIntPtr SystemCache;
            public readonly UIntPtr KernelTotal;
            public readonly UIntPtr KernelPaged;
            public readonly UIntPtr KernelNonpaged;
            public readonly UIntPtr PageSize;
            public readonly uint HandleCount;
            public readonly uint ProcessCount;
            public readonly uint ThreadCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FILETIME
        {
            public readonly uint dwLowDateTime;
            public readonly uint dwHighDateTime;

            public ulong ToUInt64()
            {
                var high = (ulong) dwHighDateTime;
                var low = (ulong) dwLowDateTime;
                return (high << 32) | low;
            }
        }
    }
}