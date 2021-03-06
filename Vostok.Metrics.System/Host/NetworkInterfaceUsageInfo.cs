﻿using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public class NetworkInterfaceUsageInfo
    {
        public string InterfaceName { get; set; }
        public long SentBytesPerSecond { get; set; }
        public long ReceivedBytesPerSecond { get; set; }
        public long BandwidthBytesPerSecond { get; set; }
    }

    [PublicAPI]
    public class TeamingInterfaceUsageInfo : NetworkInterfaceUsageInfo
    {
        public HashSet<string> ChildInterfaces { get; set; }
    }
}