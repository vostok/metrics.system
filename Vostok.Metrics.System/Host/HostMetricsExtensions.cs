using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.Models;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host;

[PublicAPI]
public static class HostMetricsExtensions
{
    /// <summary>
    /// Make MetricDataPoint from HostMetrics
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static IEnumerable<MetricDataPoint> ToDataPoints(this HostMetrics metrics, DateTimeOffset? timestamp = null)
    {
        foreach (var property in typeof(HostMetrics).GetProperties())
        {
            if (property.PropertyType.GetInterface(nameof(IDictionary)) == null)
            {
                yield return new MetricDataPoint(
                    Convert.ToDouble(property.GetValue(metrics)),
                    (WellKnownTagKeys.Name, property.Name)) {Timestamp = timestamp};
            }
        }

        foreach (var tcpState in metrics.TcpStates.OrEmptyIfNull())
        {
            yield return new MetricDataPoint(
                Convert.ToDouble(tcpState.Value),
                (WellKnownTagKeys.Name, "TcpConnectionCountPerState"),
                (nameof(TcpState), tcpState.Key.ToString())
            ) {Timestamp = timestamp};
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
                    ) {Timestamp = timestamp};
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
                    ) {Timestamp = timestamp};
                }
            }
        }
    }

    public static void LogMetrics(this HostMetrics metrics, ILog log, TimeSpan period)
    {
        log.Info(
            "CPU = {CpuUsagePercent:0.00}% ({CpuUsageCores:0.00} cores). " +
            "Memory: available = {MemoryAvailable} / {MemoryTotal}; cached = {MemoryCached}; kernel = {MemoryKernel}; Page faults = {PageFaultsPerSecond}/sec. " +
            "Processes = {ProcessCount}. Threads = {ThreadCount}. Handles = {HandleCount}. " +
            "TCP connections = {TcpConnectionsTotalCount}. " +
            "Network: in = {NetworkInUsagePercent:0.00}% ({NetworkReceivedBytesPerSecond}/sec); out = {NetworkOutUsagePercent:0.00}% ({NetworkSentBytesPerSecond}/sec).",
            new
            {
                CpuUsagePercent = metrics.CpuUtilizedFraction * 100,
                CpuUsageCores = metrics.CpuUtilizedCores,
                MemoryAvailable = SizeFormatter.Format(metrics.MemoryAvailable),
                MemoryTotal = SizeFormatter.Format(metrics.MemoryTotal),
                MemoryCached = SizeFormatter.Format(metrics.MemoryCached),
                MemoryKernel = SizeFormatter.Format(metrics.MemoryKernel),
                metrics.PageFaultsPerSecond,
                metrics.ProcessCount,
                metrics.ThreadCount,
                metrics.HandleCount,
                metrics.TcpConnectionsTotalCount,
                NetworkInUsagePercent = metrics.NetworkInUtilizedPercent,
                NetworkReceivedBytesPerSecond = SizeFormatter.Format(metrics.NetworkReceivedBytesPerSecond),
                NetworkOutUsagePercent = metrics.NetworkOutUtilizedPercent,
                NetworkSentBytesPerSecond = SizeFormatter.Format(metrics.NetworkSentBytesPerSecond)
            }
        );
    }
}