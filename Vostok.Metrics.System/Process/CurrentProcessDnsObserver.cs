using System;
using Vostok.Commons.Environment;
using Vostok.Metrics.System.Dns;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    internal class CurrentProcessDnsObserver : IObserver<DnsLookupInfo>
    {
        private readonly ConcurrentCounter lookupsCounter = new ConcurrentCounter();
        private readonly ConcurrentCounter failedLookupsCounter = new ConcurrentCounter();

        public void Collect(CurrentProcessMetrics metrics)
        {
            if (!RuntimeDetector.IsDotNet50AndNewer)
                return;
            metrics.DnsLookupsCount = lookupsCounter.CollectAndReset();
            metrics.FailedDnsLookupsCount = failedLookupsCounter.CollectAndReset();
        }

        public void OnNext(DnsLookupInfo value)
        {
            lookupsCounter.Increment();
            if (value.IsFailed)
                failedLookupsCounter.Increment();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}