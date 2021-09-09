using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public static class CurrentProcessMetricsCollectorExtensions
    {
        /// <summary>
        /// <para>Enables reporting of system metrics of the current process.</para>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// </summary>
        public static void ReportMetrics([NotNull] this CurrentProcessMetricsCollector collector, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
            => metricContext.CreateMultiFuncGauge(() => ProvideMetrics(collector), new FuncGaugeConfig {ScrapePeriod = period});

        private static IEnumerable<MetricDataPoint> ProvideMetrics(CurrentProcessMetricsCollector collector)
        {
            var metrics = collector.Collect();

            foreach (var property in typeof(CurrentProcessMetrics).GetProperties())
            {
                var value = property.GetValue(metrics);
                if (value != null)
                    yield return new MetricDataPoint(Convert.ToDouble(value), (WellKnownTagKeys.Name, property.Name));
            }
        }
    }
}