using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    /// <summary>
    /// <see cref="HostMonitor"/> provides means to observe <see cref="HostMetrics"/> periodically.
    /// </summary>
    [PublicAPI]
    public class HostMonitor : IDisposable
    {
        private readonly object guard = new object();

        private readonly HostMetricsSettings settings;

        private Dictionary<TimeSpan, DisposablePeriodicObservable<HostMetrics>> observables
            = new Dictionary<TimeSpan, DisposablePeriodicObservable<HostMetrics>>();

        public HostMonitor(HostMetricsSettings settings)
            => this.settings = settings;

        public HostMonitor()
            : this(new HostMetricsSettings())
        {
        }

        public IObservable<HostMetrics> ObserveMetrics(TimeSpan period)
        {
            lock (guard)
            {
                if (observables == null)
                    throw new ObjectDisposedException(nameof(HostMonitor));

                return observables.GetOrAdd(period, BuildObservable);
            }
        }

        public void Dispose()
        {
            lock (guard)
            {
                foreach (var disposablePeriodicObservable in observables)
                    disposablePeriodicObservable.Value.Dispose();
                observables = null;
            }
        }

        private DisposablePeriodicObservable<HostMetrics> BuildObservable(TimeSpan period)
        {
            var collector = new HostMetricsCollector(settings);
            return new DisposablePeriodicObservable<HostMetrics>(period, collector.Collect, collector.Dispose);
        }
    }
}