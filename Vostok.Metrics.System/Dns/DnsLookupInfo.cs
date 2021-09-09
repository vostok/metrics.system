using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Dns
{
    /// <summary>
    /// <see cref="DnsLookupInfo"/> Describes a single dns lookup that occurred in current process.
    /// </summary>
    [PublicAPI]
    public class DnsLookupInfo
    {
        public DnsLookupInfo(bool isFailed, TimeSpan latency)
        {
            IsFailed = isFailed;
            Latency = latency;
        }
        
        /// <summary>
        /// Dns lookup success.
        /// </summary>
        public bool IsFailed { get; }
        
        /// <summary>
        /// Latency of the dns lookup.
        /// </summary>
        public TimeSpan Latency { get; }
    }
}