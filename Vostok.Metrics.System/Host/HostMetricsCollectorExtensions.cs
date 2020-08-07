using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public static class HostMetricsCollectorExtensions
    {
        /// <summary>
        /// <para>Enables reporting of system metrics of the host.</para>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// </summary>
        public static void ReportMetrics([NotNull] this HostMetricsCollector collector,
            [NotNull] IMetricContext metricContext, TimeSpan? period = null)
            => metricContext.CreateMultiFuncGauge(() => ProvideMetrics(collector),
                new FuncGaugeConfig {ScrapePeriod = period});

        private static IEnumerable<MetricDataPoint> ProvideMetrics(HostMetricsCollector collector)
        {
            var metrics = collector.Collect();

            foreach (var property in typeof(HostMetrics).GetProperties())
            {
                if (property.PropertyType.GetInterface(nameof(IDictionary)) == null)
                {
                    yield return new MetricDataPoint(Convert.ToDouble(property.GetValue(metrics)),
                        (WellKnownTagKeys.Name, property.Name));
                }
            }

            foreach (var tcpState in metrics.TcpStates)
            {
                yield return new MetricDataPoint(
                    Convert.ToDouble(tcpState.Value),
                    (nameof(Type), nameof(HostMetrics.TcpStates)),
                    (WellKnownTagKeys.Name, tcpState.Key.ToString())
                );
            }

            foreach (var diskSpaceInfo in metrics.DisksSpaceInfo)
            {
                foreach (var property in typeof(DiskSpaceInfo).GetProperties())
                {
                    if (!property.Name.Equals(nameof(DiskSpaceInfo.DiskName)))
                    {
                        yield return new MetricDataPoint(
                            Convert.ToDouble(property.GetValue(diskSpaceInfo.Value)),
                            (WellKnownTagKeys.Name, property.Name),
                            (nameof(DiskSpaceInfo.DiskName), diskSpaceInfo.Value.DiskName)
                        );
                    }
                }
            }

            foreach (var diskUsageInfo in metrics.DisksUsageInfo)
            {
                foreach (var property in typeof(DiskUsageInfo).GetProperties())
                {
                    if (!property.Name.Equals(nameof(DiskUsageInfo.DiskName)))
                    {
                        yield return new MetricDataPoint(
                            Convert.ToDouble(property.GetValue(diskUsageInfo.Value)),
                            (WellKnownTagKeys.Name, property.Name),
                            (nameof(DiskSpaceInfo.DiskName), diskUsageInfo.Value.DiskName)
                        );
                    }
                }
            }
        }
    }
}