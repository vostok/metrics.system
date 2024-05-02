using System;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public static class CurrentProcessMonitorExtensions
    {
        private static Action<ILog, TimeSpan, CurrentProcessMetrics> loggingRule = (log, period, metrics) =>
        {
            metrics.LogMetrics(log, period);
        };

        /// <summary>
        /// <para>Enables periodical logging of current process system metrics into given <paramref name="log"/>.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
        /// </summary>
        [NotNull]
        public static IDisposable LogPeriodically([NotNull] this CurrentProcessMonitor monitor, [NotNull] ILog log, TimeSpan period)
            => monitor.ObserveMetrics(period).Subscribe(new LoggingObserver<CurrentProcessMetrics>(log, period, loggingRule));
    }
}