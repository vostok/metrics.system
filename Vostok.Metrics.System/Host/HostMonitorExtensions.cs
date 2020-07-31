using System;
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
            // TODO: Add drive info and tcp state logging.
            log.Info(
                "CPU = {CpuUsagePercent:0.00}% ({CpuUsageCores:0.00} cores). " +
                "Total Memory = {MemoryTotal} (Available = {MemoryAvailable})(Cached = {MemoryCached})(Kernel = {MemoryKernel}). " +
                "Process count = {ProcessCount}. Thread count = {ThreadCount}. Handle count = {HandleCount}" +
                "TCP connections total count = {TcpConnectionsTotalCount}",
                metrics.CpuUtilizedFraction * 100,
                metrics.CpuUtilizedCores,
                SizeFormatter.Format(metrics.MemoryTotal),
                SizeFormatter.Format(metrics.MemoryAvailable),
                SizeFormatter.Format(metrics.MemoryCached),
                SizeFormatter.Format(metrics.MemoryKernel),
                metrics.ProcessCount,
                metrics.ThreadCount,
                metrics.HandleCount,
                metrics.TcpConnectionsTotalCount
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