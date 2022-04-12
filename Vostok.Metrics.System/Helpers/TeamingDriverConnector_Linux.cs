using System;
using System.Runtime.InteropServices;

namespace Vostok.Metrics.System.Helpers
{
    // Specification: https://github.com/jpirko/libteam/wiki/Infrastructure-Specification
    // Source code: https://github.com/jpirko/libteam/blob/master/libteamdctl/libteamdctl.c
    internal class TeamingDriverConnector_Linux : IDisposable
    {
        private IntPtr teamdctlPointer = IntPtr.Zero;

        public TeamingCollector_Linux Connect(string teamingInterfaceName)
        {
            if (teamdctlPointer == IntPtr.Zero)
                InitializeTeamdctl();

            return new TeamingCollector_Linux(teamdctlPointer, teamingInterfaceName);
        }

        public void Dispose()
        {
            if (teamdctlPointer != IntPtr.Zero)
                teamdctl_free(teamdctlPointer);
        }

        private void InitializeTeamdctl()
        {
            teamdctlPointer = teamdctl_alloc();
            teamdctl_set_log_priority(teamdctlPointer, 0);
        }

        [DllImport("libteamdctl.so.0")]
        private static extern IntPtr teamdctl_alloc();

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_free(
            [In] IntPtr ctl);

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_set_log_priority(
            [In] IntPtr ctl,
            [In] int priority);
    }
}