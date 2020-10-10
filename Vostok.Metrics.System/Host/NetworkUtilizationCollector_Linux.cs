using System;
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
                var result = new NetworkInterfaceUsageInfo();

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
                toFill.ReceivedBytesPerSecond = (long)(deltaReceivedBytes / deltaSeconds);
                toFill.SentBytesPerSecond = (long)(deltaSentBytes / deltaSeconds);
            }

            toFill.BandwidthBytesPerSecond = usage.NetworkMaxMBitsPerSecond * 1000L * 1000L / 8L;
        }

        private IEnumerable<NetworkUsage> ParseNetworkUsage()
        {
            var networkInterfacesUsage = new List<NetworkUsage>();

            bool ShouldBeCounted(string interfaceName)
                => interfaceName.StartsWith("eth") || interfaceName.StartsWith("team");

            try
            {
                // NOTE: Skip first 2 lines because they contain format info. See https://man7.org/linux/man-pages/man5/proc.5.html for details.
                // NOTE: We don't need info from non ethernet interfaces.
                foreach (var line in networkUsageReader.ReadLines().Skip(2))
                {
                    if (FileParser.TrySplitLine(line, 17, out var parts) &&
                        ShouldBeCounted(parts[0]) &&
                        long.TryParse(parts[1], out var receivedBytes) &&
                        long.TryParse(parts[9], out var sentBytes))
                    {
                        networkInterfacesUsage.Add(
                            new NetworkUsage
                            {
                                InterfaceName = parts[0].TrimEnd(':'),
                                ReceivedBytes = receivedBytes,
                                SentBytes = sentBytes
                            });
                    }
                }

                // NOTE: See https://www.kernel.org/doc/Documentation/ABI/testing/sysfs-class-net for details.
                foreach (var networkUsage in networkInterfacesUsage)
                    networkUsage.NetworkMaxMBitsPerSecond = int.Parse(File.ReadAllText($"/sys/class/net/{networkUsage.InterfaceName}/speed"));
            }
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
            }

            return networkInterfacesUsage;
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