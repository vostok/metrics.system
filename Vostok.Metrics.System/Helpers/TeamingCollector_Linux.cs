using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Vostok.Metrics.System.Helpers
{
    // Specification: https://github.com/jpirko/libteam/wiki/Infrastructure-Specification
    // Source code: https://github.com/jpirko/libteam/blob/master/libteamdctl/libteamdctl.c
    internal class TeamingCollector_Linux : IDisposable
    {
        private readonly IntPtr teamdctlPointer;

        public TeamingCollector_Linux(IntPtr ctl, string teamingInterfaceName)
        {
            teamdctlPointer = ctl;

            if (teamdctl_connect(ctl, teamingInterfaceName, null, "dbus") != 0)
                throw new Exception("Unable to connect to teaming driver. Note that this operation is possible only under sudo.");
        }

        public IEnumerable<string> GetChildPorts()
        {
            var config = Marshal.PtrToStringAuto(teamdctl_state_get_raw(teamdctlPointer));

            var parsedConfig = JObject.Parse(config ?? throw new Exception("Unable to get child ports."));

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
            [In] IntPtr ctl,
            [In] string teamName,
            [In] string addr,
            [In] string cliType);

        [DllImport("libteamdctl.so.0")]
        private static extern void teamdctl_disconnect(
            [In] IntPtr ctl);

        [DllImport("libteamdctl.so.0")]
        private static extern IntPtr teamdctl_state_get_raw(
            [In] IntPtr ctl);

        [DllImport("libteamdctl.so.0")]
        private static extern int teamdctl_state_item_value_get(
            [In] IntPtr ctl,
            [In] string itemPath,
            [Out] out string value);
    }
}