using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Dns
{
    [PublicAPI]
    public class DnsMetrics
    {
        public DnsMetrics(long lookupsCount, long failedLookups, long averageLookupDuration)
        {
            LookupsCount = lookupsCount;
            FailedLookups = failedLookups;
            AverageLookupDuration = averageLookupDuration;
        }
        
        public long LookupsCount { get; }
        
        public long FailedLookups { get; }
        
        public long AverageLookupDuration { get; }
    }
}