using System;
using JetBrains.Annotations;
using Vostok.Metrics.Primitives.Timer;

namespace Vostok.Metrics.System.Dns
{
    [PublicAPI]
    public static class DnsMonitorExtensions_Metrics
    {
        /// <summary>
        /// <para>Enables reporting summary of DNS lookup time.</para>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
        /// </summary>
        [NotNull]
        public static IDisposable ReportMetrics([NotNull] this DnsMonitor monitor, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
            => monitor.Subscribe(new ReportingObserver(metricContext, period));

        private class ReportingObserver : IObserver<DnsLookupInfo>
        {
            private readonly ITimer lookupLatency;

            public ReportingObserver(IMetricContext metricContext, TimeSpan? period)
            {
                lookupLatency = metricContext.CreateSummary("DnsLookupLatency", new SummaryConfig {Unit = WellKnownUnits.Milliseconds, ScrapePeriod = period});
            }

            public void OnNext(DnsLookupInfo value)
            {
                lookupLatency.Report(value.Latency.TotalMilliseconds);
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}