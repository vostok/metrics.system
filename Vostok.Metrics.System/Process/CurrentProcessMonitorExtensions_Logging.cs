using System;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public static class CurrentProcessMonitorExtensions_Logging
    {
        /// <summary>
        /// <para>Enables periodical logging of current process system metrics into given <paramref name="log"/>.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
        /// </summary>
        [NotNull]
        public static IDisposable LogPeriodically([NotNull] this CurrentProcessMonitor monitor, [NotNull] ILog log, TimeSpan period)
            => monitor.ObserveMetrics(period).Subscribe(new LoggingObserver(log, period));

        private class LoggingObserver : IObserver<CurrentProcessMetrics>
        {
            private readonly ILog log;
            private readonly TimeSpan period;

            public LoggingObserver(ILog log, TimeSpan period)
            {
                this.period = period;
                this.log = log.ForContext<CurrentProcessMetrics>();
            }

            public void OnNext(CurrentProcessMetrics metrics)
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

            public void OnError(Exception error)
                => log.Warn(error);

            public void OnCompleted()
            {
            }
        }
    }
}
