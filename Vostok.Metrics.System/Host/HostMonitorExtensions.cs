using System;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public static class HostMonitorExtensions
    {
        private static Action<ILog, TimeSpan, HostMetrics> loggingRule = (log, period, metrics) => { throw new NotImplementedException(); };

        /// <summary>
        /// <para>Enables periodical logging of host system metrics into given <paramref name="log"/>.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
        /// </summary>
        [NotNull]
        public static IDisposable LogPeriodically([NotNull] this HostMonitor monitor, [NotNull] ILog log, TimeSpan period)
            => monitor.ObserveMetrics(period).Subscribe(new LoggingObserver<HostMetrics>(log, period, loggingRule));
    }
}