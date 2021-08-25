using System;
using JetBrains.Annotations;
using Vostok.Metrics.Primitives.Counter;
using Vostok.Metrics.Primitives.Timer;

namespace Vostok.Metrics.System.Dns
{
    [PublicAPI]
    public static class DnsMonitorExtensions_Metrics
    {
        /// <summary>
        /// <para>Enables reporting of following dns lookups metrics:</para>
        /// <list type="bullet">
        ///     <item><description>Percentile of time to resolve dns lookup.</description></item>
        ///     <item><description>Total lookups count (sum of all lookups for the reporting period).</description></item>
        ///     <item><description>Failed lookups count(sum of all failed lookups for the reporting period).</description></item>
        /// </list>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
        /// </summary>
        [NotNull]
        public static IDisposable ReportMetrics([NotNull] this DnsMonitor monitor, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
            => monitor.Subscribe(new ReportingObserver(metricContext, period));

        private class ReportingObserver : IObserver<DnsLookupInfo>
        {
            private readonly ITimer lookupLatency;
            private readonly ICounter lookupCount;
            private readonly ICounter failedLookupCount;

            public ReportingObserver(IMetricContext metricContext, TimeSpan? period)
            {
                lookupLatency = metricContext.CreateSummary("DnsLookupLatency", new SummaryConfig {Unit = WellKnownUnits.Milliseconds, ScrapePeriod = period});

                var counterConfig = new CounterConfig {Unit = WellKnownUnits.None, ScrapePeriod = period};
                lookupCount = metricContext.CreateCounter("DnsLookupCount", counterConfig);
                failedLookupCount = metricContext.CreateCounter("FailedDnsLookupCount", counterConfig);
            }

            public void OnNext(DnsLookupInfo value)
            {
                lookupCount.Increment();
                if (value.IsFailed)
                    failedLookupCount.Increment();

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