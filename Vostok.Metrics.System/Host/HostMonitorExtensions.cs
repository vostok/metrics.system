﻿using System;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public static class HostMonitorExtensions
    {
        private static Action<ILog, TimeSpan, HostMetrics> loggingRule = (log, period, metrics) =>
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
        };

        /// <summary>
        /// <para>Enables periodical logging of host system metrics into given <paramref name="log"/>.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
        /// </summary>
        [NotNull]
        public static IDisposable LogPeriodically([NotNull] this HostMonitor monitor, [NotNull] ILog log, TimeSpan period)
            => monitor.ObserveMetrics(period).Subscribe(new LoggingObserver<HostMetrics>(log, period, loggingRule));
    }
}