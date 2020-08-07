using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Vostok.Metrics.System.Helpers
{
    internal class WinMetricsCollectorHelper
    {
        public static void ThrowOnError()
        {
            var exception = new Win32Exception();

            throw new Win32Exception(exception.ErrorCode, exception.Message + $" Error code = {exception.ErrorCode} (0x{exception.ErrorCode:X}).");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(
            out FILETIME idleTime,
            out FILETIME kernelTime,
            out FILETIME userTime);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
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