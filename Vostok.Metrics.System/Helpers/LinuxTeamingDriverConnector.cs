using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Vostok.Metrics.System.Helpers
{
    // Specification: https://github.com/jpirko/libteam/wiki/Infrastructure-Specification
    // Source code: https://github.com/jpirko/libteam/blob/master/libteamdctl/libteamdctl.c
    internal class LinuxTeamingDriverConnector : IDisposable
    {
        private readonly UIntPtr teamdctlPointer;

        public LinuxTeamingDriverConnector()
        {
            teamdctlPointer = teamdctl_alloc();
        }

        public TeamingCollector Connect(string teamingInterfaceName)
        {
            return new TeamingCollector(teamdctlPointer, teamingInterfaceName);
        }

        public void Dispose()
        {
            teamdctl_free(teamdctlPointer);
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
        private readonly UIntPtr teamdctlPointer;

        public TeamingCollector(UIntPtr ctl, string teamingInterfaceName)
        {
            teamdctlPointer = ctl;
            teamdctl_connect(ctl, teamingInterfaceName, null, null);
        }

        public IEnumerable<string> GetChildPorts()
        {
            var config = teamdctl_state_get_raw(teamdctlPointer);
            
            var parsedConfig = JObject.Parse(config);

            return parsedConfig.Value<JObject>("ports").Properties().Select(x => x.Name);
        }

        public string GetTeamingMode()
        {
            if (teamdctl_state_item_value_get(teamdctlPointer, "setup.runner_name", out var value) == 0)
                return value;

            throw new Exception("Unable to get current teaming mode.");
        }

        public string GetActivebackupRunnerPort()
        {
            if (teamdctl_state_item_value_get(teamdctlPointer, "runner.active_port", out var value) == 0)
                return value;

            throw new Exception("Unable to get current activebackup runner port.");
        }

        public void Dispose()
        {
            teamdctl_disconnect(teamdctlPointer);
        }

        [DllImport("libteamdctl.so.0")]
        private static extern int teamdctl_connect(
            [In] UIntPtr ctl,
            [In] string teamName,
            [In] string addr,
            [In] string ctlType);

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_disconnect(
            [In] UIntPtr ctl);

        [DllImport("libteamdctl.so.0")]
        private static extern string teamdctl_state_get_raw(
            [In] UIntPtr ctl);

        [DllImport("libteamdctl.so.0")]
        private static extern int teamdctl_state_item_value_get(
            [In] UIntPtr ctl,
            [In] string itemPath,
            [Out] out string value);
    }
}