using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class NetworkUtilizationCollector_Linux : IDisposable
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly ReusableFileReader networkUsageReader = new ReusableFileReader("/proc/net/dev");
        private volatile Dictionary<string, NetworkUsage> previousNetworkUsageInfo = new Dictionary<string, NetworkUsage>();
        private readonly Regex teamingModeRegex = new Regex("(activebackup)*(roundrobin)*(broadcast)*(loadbalance)*(random)*(lacp)*", RegexOptions.Compiled);


        public void Dispose()
        {
            networkUsageReader?.Dispose();
        }

        public void Collect(HostMetrics metrics)
        {
            var deltaSeconds = stopwatch.Elapsed.TotalSeconds;

            var newNetworkUsageInfo = new Dictionary<string, NetworkUsage>();
            var networkInterfacesUsageInfo = new Dictionary<string, NetworkInterfaceUsageInfo>();

            foreach (var networkUsage in ParseNetworkUsage())
            {
                var result = new NetworkInterfaceUsageInfo {InterfaceName = networkUsage.InterfaceName};

                if (previousNetworkUsageInfo.TryGetValue(networkUsage.InterfaceName, out var value))
                    FillInfo(result, value, networkUsage, deltaSeconds);

                newNetworkUsageInfo[networkUsage.InterfaceName] = networkUsage;

                networkInterfacesUsageInfo[networkUsage.InterfaceName] = result;
            }

            metrics.NetworkInterfacesUsageInfo = networkInterfacesUsageInfo;

            previousNetworkUsageInfo = newNetworkUsageInfo;

            stopwatch.Restart();
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
            // NOTE: 'team' stands for teaming.
            bool ShouldBeCounted(string interfaceName)
                => interfaceName.StartsWith("eth") || 
                   interfaceName.StartsWith("en") || 
                   interfaceName.StartsWith("team");

            IEnumerable<NetworkUsage> FilterDisabledInterfaces(IEnumerable<NetworkUsage> interfaceUsages)
            {
                return interfaceUsages.Where(x => x.NetworkMaxMBitsPerSecond != -1);
            }

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
                        var networkUsage = new NetworkUsage
                        {
                            InterfaceName = parts[0].TrimEnd(':'),
                            ReceivedBytes = receivedBytes,
                            SentBytes = sentBytes
                        };
                        networkInterfacesUsage[networkUsage.InterfaceName] = networkUsage;
                    }
                }

                // NOTE: See https://www.kernel.org/doc/Documentation/ABI/testing/sysfs-class-net for details.
                // NOTE: This value equals -1 if interface is disabled, so we will filter this values out later.
                foreach (var networkUsage in networkInterfacesUsage.Values)
                {
                    if (int.TryParse(File.ReadAllText($"/sys/class/net/{networkUsage.InterfaceName}/speed"), out var speed))
                        networkUsage.NetworkMaxMBitsPerSecond = speed;
                    else
                    {
                        // NOTE: We don't need zero values. Mark this interface disabled for now. It's speed may be calculated later if it is teaming interface.
                        networkUsage.NetworkMaxMBitsPerSecond = -1; 
                    }
                }

                foreach (var teamingInterface in networkInterfacesUsage.Values.Where(x => x.InterfaceName.StartsWith("team") && x.NetworkMaxMBitsPerSecond == -1))
                {
                    
                }
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return FilterDisabledInterfaces(networkInterfacesUsage.Values);
        }

        private IEnumerable<string> GetTeamingChildInterfaces(string teamingInterface)
        {
            yield break;
        }

        // We don't handle nested teaming interfaces.
        // We ignore disabled interfaces.
        private long CalculateTeamingSpeed(string teamingMode, Dictionary<string, NetworkUsage> usages)
        {
            return 0;
        }

        private class NetworkUsage
        {
            public string InterfaceName { get; set; }
            public long ReceivedBytes { get; set; }
            public long SentBytes { get; set; }
            public long NetworkMaxMBitsPerSecond { get; set; }
        }
    }
}