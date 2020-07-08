using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public class CurrentProcessMonitor
    {
        private readonly ConcurrentDictionary<TimeSpan, PeriodicObservable<CurrentProcessMetrics>> observables 
            = new ConcurrentDictionary<TimeSpan, PeriodicObservable<CurrentProcessMetrics>>();

        public IObservable<CurrentProcessMetrics> ObserveMetrics(TimeSpan period)
            => observables.GetOrAdd(period, p => new PeriodicObservable<CurrentProcessMetrics>(p, new CurrentProcessMetricsCollector().Collect));
    }
}
