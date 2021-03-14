﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class NetworkUtilizationCollector_Linux : IDisposable
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly ReusableFileReader networkUsageReader = new ReusableFileReader("/proc/net/dev");
        private volatile Dictionary<string, NetworkUsage> previousNetworkUsageInfo = new Dictionary<string, NetworkUsage>();
        private readonly LinuxTeamingDriverConnector teamingConnector = new LinuxTeamingDriverConnector();

        public void Dispose()
        {
            networkUsageReader?.Dispose();
            teamingConnector?.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            var deltaSeconds = stopwatch.Elapsed.TotalSeconds;

            var newNetworkUsageInfo = new Dictionary<string, NetworkUsage>();
            var networkInterfacesUsageInfo = new Dictionary<string, NetworkInterfaceUsageInfo>();

            foreach (var networkUsage in ParseNetworkUsage())
            {
                var result = CreateInfo(networkUsage);

                if (previousNetworkUsageInfo.TryGetValue(networkUsage.InterfaceName, out var value))
                    FillInfo(result, value, networkUsage, deltaSeconds);

                newNetworkUsageInfo[networkUsage.InterfaceName] = networkUsage;

                networkInterfacesUsageInfo[networkUsage.InterfaceName] = result;
            }

            metrics.NetworkInterfacesUsageInfo = networkInterfacesUsageInfo;

            previousNetworkUsageInfo = newNetworkUsageInfo;

            stopwatch.Restart();
        }

        private NetworkInterfaceUsageInfo CreateInfo(NetworkUsage usage)
        {
            NetworkInterfaceUsageInfo result;

            if (usage is TeamingNetworkUsage teamingNetworkUsage)
                result = new TeamingInterfaceUsageInfo {ChildInterfaces = teamingNetworkUsage.ChildInterfaces};
            else
                result = new NetworkInterfaceUsageInfo();

            result.InterfaceName = usage.InterfaceName;

            return result;
        }

        private void FillInfo(NetworkInterfaceUsageInfo toFill, NetworkUsage previousUsage, NetworkUsage usage, double deltaSeconds)
        {
            var deltaReceivedBytes = usage.ReceivedBytes - previousUsage.ReceivedBytes;
            var deltaSentBytes = usage.SentBytes - previousUsage.SentBytes;

            if (deltaSeconds > 0d)
            {
                toFill.ReceivedBytesPerSecond = (long) (deltaReceivedBytes / deltaSeconds);
                toFill.SentBytesPerSecond = (long) (deltaSentBytes / deltaSeconds);
            }

            toFill.BandwidthBytesPerSecond = usage.NetworkMaxMBitsPerSecond * 1000L * 1000L / 8L;
        }

        private IEnumerable<NetworkUsage> ParseNetworkUsage()
        {
            var networkInterfacesUsage = new Dictionary<string, NetworkUsage>();

            // NOTE: 'eth' stands for ethernet interface.
            // NOTE: 'en' stands for ethernet interface in 'Predictable network interface device names scheme'. 
            bool ShouldBeCounted(string interfaceName)
                => interfaceName.StartsWith("eth") ||
                   interfaceName.StartsWith("en") ||
                   IsTeamingInterface(interfaceName);

            IEnumerable<NetworkUsage> FilterDisabledInterfaces(IEnumerable<NetworkUsage> interfaceUsages)
                => interfaceUsages.Where(x => x.NetworkMaxMBitsPerSecond != -1);

            NetworkUsage CreateNetworkUsage(string interfaceName, long receivedBytes, long sentBytes)
            {
                if (IsTeamingInterface(interfaceName))
                {
                    return new TeamingNetworkUsage
                    {
                        InterfaceName = interfaceName,
                        ReceivedBytes = receivedBytes,
                        SentBytes = sentBytes
                    };
                }

                return new NetworkUsage
                {
                    InterfaceName = interfaceName,
                    ReceivedBytes = receivedBytes,
                    SentBytes = sentBytes
                };
            }

            // TODO: Remember all interfaces that are used in teaming and calculate total speed accordingly.
            try
            {
                // NOTE: Skip first 2 lines because they contain format info. See https://man7.org/linux/man-pages/man5/proc.5.html for details.
                foreach (var line in networkUsageReader.ReadLines().Skip(2))
                {
                    if (FileParser.TrySplitLine(line, 17, out var parts) &&
                        ShouldBeCounted(parts[0]) &&
                        long.TryParse(parts[1], out var receivedBytes) &&
                        long.TryParse(parts[9], out var sentBytes))
                    {
                        var interfaceName = parts[0].TrimEnd(':');
                        networkInterfacesUsage[interfaceName] = CreateNetworkUsage(interfaceName, receivedBytes, sentBytes);
                    }
                }

                // NOTE: See https://www.kernel.org/doc/Documentation/ABI/testing/sysfs-class-net for details.
                // NOTE: This value equals -1 if interface is disabled, so we will filter this values out later.
                foreach (var networkUsage in networkInterfacesUsage.Values)
                {
                    if (TryRead($"/sys/class/net/{networkUsage.InterfaceName}/speed", out var result) && int.TryParse(result, out var speed))
                        networkUsage.NetworkMaxMBitsPerSecond = speed;
                    else
                    {
                        // NOTE: We don't need zero values. Mark this interface disabled for now. It's speed may be calculated later if it is a teaming interface.
                        networkUsage.NetworkMaxMBitsPerSecond = -1;
                    }
                }

                var teamingInterfaces = networkInterfacesUsage.Values
                   .Where(x => IsTeamingInterface(x.InterfaceName) && x.NetworkMaxMBitsPerSecond == -1)
                   .Cast<TeamingNetworkUsage>();

                foreach (var teamingInterface in teamingInterfaces)
                    FillTeamingInfo(networkInterfacesUsage, teamingInterface);
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return FilterDisabledInterfaces(networkInterfacesUsage.Values);
        }

        private void FillTeamingInfo(IReadOnlyDictionary<string, NetworkUsage> usages, TeamingNetworkUsage teamingUsage)
        {
            using (var collector = teamingConnector.Connect(teamingUsage.InterfaceName))
            {
                var teamingMode = collector.GetTeamingMode();
                teamingUsage.ChildInterfaces = new HashSet<string>(collector.GetChildPorts());

                // We don't handle nested teaming interfaces.
                if (!teamingUsage.ChildInterfaces.All(x => !IsTeamingInterface(x)))
                    throw new NotSupportedException("Nested teaming interfaces are not supported.");

                // We ignore disabled interfaces.
                var childSpeeds = teamingUsage.ChildInterfaces
                   .Select(x => usages[x].NetworkMaxMBitsPerSecond)
                   .Where(x => x > 0);

                switch (teamingMode)
                {
                    case "activebackup":
                        var activePort = collector.GetActivebackupRunnerPort();
                        teamingUsage.NetworkMaxMBitsPerSecond = usages[activePort].NetworkMaxMBitsPerSecond;
                        return;
                    case "roundrobin":
                    case "random":
                    case "broadcast":
                        teamingUsage.NetworkMaxMBitsPerSecond = childSpeeds.Min();
                        return;
                    case "loadbalance":
                    case "lacp":
                        teamingUsage.NetworkMaxMBitsPerSecond = childSpeeds.Sum();
                        return;
                    default:
                        throw new ArgumentException($"Unknown teaming mode {teamingMode}.");
                }
            }
        }

        private static bool TryRead(string path, out string result)
        {
            try
            {
                result = File.ReadAllText(path);
                return true;
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);

                result = null;
                return false;
            }
        }

        private static bool IsTeamingInterface(string interfaceName) => interfaceName.StartsWith("team");

        private class NetworkUsage
        {
            public string InterfaceName { get; set; }
            public long ReceivedBytes { get; set; }
            public long SentBytes { get; set; }
            public long NetworkMaxMBitsPerSecond { get; set; }
        }

        private class TeamingNetworkUsage : NetworkUsage
        {
            public HashSet<string> ChildInterfaces { get; set; }
        }
    }
}