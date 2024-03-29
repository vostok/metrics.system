﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public static class HostMetricsCollectorExtensions
    {
        /// <summary>
        /// <para>Enables reporting of system metrics of the host.</para>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// </summary>
        public static IDisposable ReportMetrics(
            [NotNull] this HostMetricsCollector collector,
            [NotNull] IMetricContext metricContext,
            TimeSpan? period = null)
            => metricContext.CreateMultiFuncGauge(
                () => ProvideMetrics(collector),
                new FuncGaugeConfig {ScrapePeriod = period}) as IDisposable;

        private static IEnumerable<MetricDataPoint> ProvideMetrics(HostMetricsCollector collector)
        {
            var metrics = collector.Collect();

            foreach (var property in typeof(HostMetrics).GetProperties())
            {
                if (property.PropertyType.GetInterface(nameof(IDictionary)) == null)
                {
                    yield return new MetricDataPoint(
                        Convert.ToDouble(property.GetValue(metrics)),
                        (WellKnownTagKeys.Name, property.Name));
                }
            }

            foreach (var tcpState in metrics.TcpStates.OrEmptyIfNull())
            {
                yield return new MetricDataPoint(
                    Convert.ToDouble(tcpState.Value),
                    (WellKnownTagKeys.Name, "TcpConnectionCountPerState"),
                    (nameof(TcpState), tcpState.Key.ToString())
                );
            }

            foreach (var diskSpaceInfo in metrics.DisksSpaceInfo.OrEmptyIfNull())
            {
                foreach (var property in typeof(DiskSpaceInfo).GetProperties())
                {
                    if (!property.Name.Equals(nameof(DiskSpaceInfo.DiskName)) && !property.Name.Equals(nameof(DiskSpaceInfo.RootDirectory)))
                    {
                        yield return new MetricDataPoint(
                            Convert.ToDouble(property.GetValue(diskSpaceInfo.Value)),
                            (WellKnownTagKeys.Name, $"Disk{property.Name}"),
                            (nameof(DiskSpaceInfo.DiskName), diskSpaceInfo.Value.DiskName)
                        );
                    }
                }
            }

            foreach (var diskUsageInfo in metrics.DisksUsageInfo.OrEmptyIfNull())
            {
                foreach (var property in typeof(DiskUsageInfo).GetProperties())
                {
                    if (!property.Name.Equals(nameof(DiskUsageInfo.DiskName)))
                    {
                        yield return new MetricDataPoint(
                            Convert.ToDouble(property.GetValue(diskUsageInfo.Value)),
                            (WellKnownTagKeys.Name, $"Disk{property.Name}"),
                            (nameof(DiskSpaceInfo.DiskName), diskUsageInfo.Value.DiskName)
                        );
                    }
                }
            }
        }
    }
}