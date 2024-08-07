﻿using System;
using System.Runtime.InteropServices;
using Vostok.Commons.Environment;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Host;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Local

namespace Vostok.Metrics.System.Process
{
    internal class NativeProcessMetricsCollector_Windows
    {
        private static readonly IntPtr CurrentProcessHandle = GetCurrentProcess();
        private static readonly bool isDotNet60AndNewer = RuntimeDetector.IsDotNet60AndNewer;

        private readonly CpuUtilizationCollector cpuCollector = new CpuUtilizationCollector();

        public void Collect(CurrentProcessMetrics metrics)
        {
            CollectMemoryMetrics(metrics);
            CollectHandlesCount(metrics);
            CollectCpuUtilization(metrics);
        }

        private static unsafe void CollectMemoryMetrics(CurrentProcessMetrics metrics)
        {
            try
            {
                if (!GetProcessMemoryInfo(CurrentProcessHandle, out var memoryCounters, sizeof(PROCESS_MEMORY_COUNTERS_EX)))
                    WinMetricsCollectorHelper.ThrowOnError();

                metrics.MemoryResident = (long)memoryCounters.WorkingSetSize;
                metrics.MemoryPrivate = (long)memoryCounters.PrivateUsage;
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }
        }

        private static void CollectHandlesCount(CurrentProcessMetrics metrics)
        {
            try
            {
                if (!GetProcessHandleCount(CurrentProcessHandle, out var handleCount))
                    WinMetricsCollectorHelper.ThrowOnError();

                metrics.HandlesCount = (int)handleCount;
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool GetProcessHandleCount(IntPtr hProcess, out uint dwHandleCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessTimes(
            IntPtr hProcess,
            out WinMetricsCollectorHelper.FILETIME creationTime,
            out WinMetricsCollectorHelper.FILETIME exitTime,
            out WinMetricsCollectorHelper.FILETIME kernelTime,
            out WinMetricsCollectorHelper.FILETIME userTime);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(
            [In] IntPtr process,
            [Out] out PROCESS_MEMORY_COUNTERS_EX ppsmemCounters,
            [In] int cb);

        private void CollectCpuUtilization(CurrentProcessMetrics metrics)
        {
            try
            {
                if (!WinMetricsCollectorHelper.GetSystemTimes(out _, out var systemKernel, out var systemUser))
                    WinMetricsCollectorHelper.ThrowOnError();

                if (!GetProcessTimes(CurrentProcessHandle, out _, out _, out var processKernel, out var processUser))
                    WinMetricsCollectorHelper.ThrowOnError();

                var systemTime = systemKernel.ToUInt64() + systemUser.ToUInt64();
                var processTime = processKernel.ToUInt64() + processUser.ToUInt64();

                if (isDotNet60AndNewer)
                {
                    // note: Environment.ProcessorCount from NET6+ respects process affinity and the job object's hard limit on CPU utilization
                    // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/environment-processorcount-on-windows
                    // so we should get total number of system cores from NativeHostMetricsCollector_Windows instead
                    cpuCollector.Collect(metrics, systemTime, processTime, NativeHostMetricsCollector_Windows.GetHostProcessorCount());
                }
                else
                {
                    //fix old behaviour before net6
                    cpuCollector.Collect(metrics, systemTime, processTime, Environment.ProcessorCount);
                }
            }
            catch (Exception error)
            {
                InternalLogger.Warn(error);
            }
        }

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
    }
}