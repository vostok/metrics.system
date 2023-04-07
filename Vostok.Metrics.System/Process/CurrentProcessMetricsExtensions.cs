using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.Models;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process;

[PublicAPI]
public static class CurrentProcessMetricsExtensions
{
    public static IEnumerable<MetricDataPoint> ToDataPoints(this CurrentProcessMetrics metrics, DateTimeOffset? timestamp = null)
    {
        foreach (var property in typeof(CurrentProcessMetrics).GetProperties())
        {
            var value = property.GetValue(metrics);

            if (value != null)
            {
                var metricDataPoint = new MetricDataPoint(Convert.ToDouble(value), (WellKnownTagKeys.Name, property.Name))
                {
                    Timestamp = timestamp
                };
                yield return metricDataPoint;
            }
        }
    }

    public static void LogMetrics(this CurrentProcessMetrics metrics, ILog log, TimeSpan period)
    {
        log.Info(
            "CPU = {CpuUsagePercent:0.00}% ({CpuUsageCores:0.00} cores). " +
            "Memory = {MemoryResidentUsage} ({GcHeapSize} managed). " +
            "ThreadPool: {ThreadPoolTotalCount} total, {ThreadPoolBusyWorkers}/{ThreadPoolMinWorkers} workers, " +
            "{ThreadPoolBusyIo}/{ThreadPoolMinIo} IO, {ThreadPoolQueueLength} queue. " +
            "Handles = {HandlesCount}. Timers = {TimersCount}. " +
            "Alloc/s = {AllocatedPerSecond:0.00}. " +
            "Contentions/s = {ContentionsPerSecond:0.00}. " +
            "Exceptions/s = {ExceptionsPerSecond:0.00}.",
            metrics.CpuUtilizedFraction * 100,
            metrics.CpuUtilizedCores,
            SizeFormatter.Format(metrics.MemoryResident),
            SizeFormatter.Format(metrics.GcHeapSize),
            metrics.ThreadPoolTotalCount,
            metrics.ThreadPoolBusyWorkers,
            metrics.ThreadPoolMinWorkers,
            metrics.ThreadPoolBusyIo,
            metrics.ThreadPoolMinIo,
            metrics.ThreadPoolQueueLength,
            metrics.HandlesCount,
            metrics.ActiveTimersCount,
            SizeFormatter.Format((long)(metrics.GcAllocatedBytes / period.TotalSeconds)),
            metrics.LockContentionCount / period.TotalSeconds,
            metrics.ExceptionsCount / period.TotalSeconds
        );
    }
}