using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    public class NativeHostMetricsCollector_Windows
    {
        private readonly HostCpuUtilizationCollector cpuCollector = new HostCpuUtilizationCollector();

        public void Collect(HostMetrics metrics)
        {
            CollectCpuUtilization(metrics);
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