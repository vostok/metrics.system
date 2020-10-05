using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    /// <summary>
    /// <see cref="CurrentProcessMonitor"/> provides means to observe <see cref="CurrentProcessMetrics"/> periodically.
    /// </summary>
    [PublicAPI]
    public class CurrentProcessMonitor
    {
        private readonly ConcurrentDictionary<TimeSpan, PeriodicObservable<CurrentProcessMetrics>> observables 
            = new ConcurrentDictionary<TimeSpan, PeriodicObservable<CurrentProcessMetrics>>();

        private readonly CurrentProcessMetricsSettings settings;

        public CurrentProcessMonitor(CurrentProcessMetricsSettings settings)
            => this.settings = settings;

        public CurrentProcessMonitor()
            : this (new CurrentProcessMetricsSettings()) { }

        public IObservable<CurrentProcessMetrics> ObserveMetrics(TimeSpan period)
            => observables.GetOrAdd(period, p => new PeriodicObservable<CurrentProcessMetrics>(p, new CurrentProcessMetricsCollector(settings).Collect));
    }
}
