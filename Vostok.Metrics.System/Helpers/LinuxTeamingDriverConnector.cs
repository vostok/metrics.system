using System;
using System.Runtime.InteropServices;

namespace Vostok.Metrics.System.Helpers
{
    internal class LinuxTeamingDriverConnector : IDisposable
    {
        private readonly UIntPtr teamdcliPointer;
        
        public LinuxTeamingDriverConnector()
        {
            teamdcliPointer = teamdctl_alloc();
        }
        
        public TeamingCollector Connect(string teamingInterfaceName)
        {
            return new TeamingCollector(teamdcliPointer, teamingInterfaceName);
        }

        public void Dispose()
        {
            teamdctl_free(teamdcliPointer);
        }

        [DllImport("libteamdctl.so.0")]
        private static extern UIntPtr teamdctl_alloc();

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_free(
            [In] UIntPtr ctl);
        
        // TODO: Redirect logging.
    }

    internal class TeamingCollector : IDisposable
    {
        private readonly UIntPtr teamdcliPointer;

        public TeamingCollector(UIntPtr cli, string teamingInterfaceName)
        {
            teamdcliPointer = cli;
            teamdctl_connect(cli, teamingInterfaceName, null, null);
        }
        
        public string GetCurrentState()
        {
            // TODO: Get current state using 'state view'
            return teamdctl_config_actual_get_raw(teamdcliPointer);
        }

        public string GetTeamingMode()
        {
            // TODO: Get mode using 'state item get setup.runner_name'
            return teamdctl_config_actual_get_raw(teamdcliPointer);
        }

        public void Dispose()
        {
            teamdctl_disconnect(teamdcliPointer);
        }
        
        [DllImport("libteamdctl.so.0")]
        private static extern int teamdctl_connect(
            [In] UIntPtr cli,
            [In] string teamName,
            [In] string addr,
            [In] string cliType);

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_disconnect(
            [In] UIntPtr cli);

        [DllImport("libteamdctl.so.0")]
        private static extern string teamdctl_config_actual_get_raw(
            [In] UIntPtr cli);
    }
}