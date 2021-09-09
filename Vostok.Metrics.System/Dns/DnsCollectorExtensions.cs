using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.System.Dns
{
    public static class DnsCollectorExtensions
    {
        public static void ReportMetrics([NotNull] this DnsCollector collector, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
            => metricContext.CreateMultiFuncGauge(() => ProvideMetrics(collector), new FuncGaugeConfig {ScrapePeriod = period});

        private static IEnumerable<MetricDataPoint> ProvideMetrics(DnsCollector collector)
        {
            var metrics = collector.Collect();

            foreach (var property in typeof(DnsMetrics).GetProperties())
                yield return new MetricDataPoint(Convert.ToDouble(property.GetValue(metrics)), (WellKnownTagKeys.Name, property.Name));
        }
    }
}