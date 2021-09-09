using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Dns
{
    [PublicAPI]
    public class DnsCollector : EventListener
    {
        private const string SourceName = "System.Net.NameResolution";

        private long lookupsCounter;
        private long failedLookupsCounter;
        private long duration;

        #region EventConstant

        private const int ResolutionStartEventId = 1;
        private const int ResolutionFailedEventId = 3;
        private const string AverageDurationCounterName = "dns-lookups-duration";

        #endregion

        public DnsMetrics Collect()
        {
            var metrics = new DnsMetrics(lookupsCounter, failedLookupsCounter, duration);
            Interlocked.Exchange(ref lookupsCounter, 0);
            Interlocked.Exchange(ref failedLookupsCounter, 0);
            return metrics;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (SourceName == eventSource.Name)
                EnableEvents(eventSource,
                    EventLevel.Verbose,
                    EventKeywords.All,
                    new Dictionary<string, string>
                    {
                        {"EventCounterIntervalSec", "10"}
                    });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var id = eventData.EventId;
            if (eventData.EventName != "EventCounters")
                switch (id)
                {
                    case ResolutionStartEventId:
                        Interlocked.Increment(ref lookupsCounter);
                        break;
                    case ResolutionFailedEventId:
                        Interlocked.Increment(ref failedLookupsCounter);
                        break;
                }
            else
            {
                if (eventData.Payload?.Count <= 0
                    || !(eventData.Payload?[0] is IDictionary<string, object> data)
                    || !data.TryGetValue("Name", out var n)
                    || !(n is string name)
                    || name != AverageDurationCounterName) return;

                if (data.TryGetValue("Mean", out var mean))
                    Interlocked.Exchange(ref duration, Convert.ToInt64(mean));
            }
        }
    }
}