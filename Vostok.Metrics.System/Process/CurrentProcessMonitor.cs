using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    /// <summary>
    /// <see cref="CurrentProcessMonitor"/> provides means to observe <see cref="CurrentProcessMetrics"/> periodically.
    /// </summary>
    [PublicAPI]
    public class CurrentProcessMonitor : IDisposable
    {
        private readonly object guard = new object();

        private readonly CurrentProcessMetricsSettings settings;

        private Dictionary<TimeSpan, DisposablePeriodicObservable<CurrentProcessMetrics>> observables
            = new Dictionary<TimeSpan, DisposablePeriodicObservable<CurrentProcessMetrics>>();

        public CurrentProcessMonitor(CurrentProcessMetricsSettings settings)
            => this.settings = settings;

        public CurrentProcessMonitor()
            : this(new CurrentProcessMetricsSettings())
        {
        }

        public IObservable<CurrentProcessMetrics> ObserveMetrics(TimeSpan period)
        {
            lock (guard)
            {
                if (observables == null)
                    throw new ObjectDisposedException(nameof(CurrentProcessMonitor));

                return observables.GetOrAdd(period, BuildObservable);
            }
        }

        public void Dispose()
        {
            Task.Run(() =>
            {
                lock (guard)
                {
                    foreach (var disposablePeriodicObservable in observables)
                        disposablePeriodicObservable.Value.Dispose();
                    observables = null;
                }
            });
        }

        private DisposablePeriodicObservable<CurrentProcessMetrics> BuildObservable(TimeSpan period)
        {
            var collector = new CurrentProcessMetricsCollector(settings);
            return new DisposablePeriodicObservable<CurrentProcessMetrics>(period, collector.Collect, collector.Dispose);
        }
    }
}