using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Dns
{
    [PublicAPI]
    public static class DnsMonitorExtensions_Logging
    {
        /// <summary>
        /// <para>Enables logging of all dns lookups matched by given <paramref name="filter"/> into provided <paramref name="log"/> with Info level.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
        /// </summary>
        [NotNull]
        public static IDisposable LogLookups([NotNull] this DnsMonitor monitor, [NotNull] ILog log, [CanBeNull] Predicate<DnsLookupInfo> filter)
            => monitor.Subscribe(new LoggingObserver(log, filter));

        private class LoggingObserver : IObserver<DnsLookupInfo>
        {
            private readonly ILog log;
            private readonly Predicate<DnsLookupInfo> filter;

            public LoggingObserver(ILog log, Predicate<DnsLookupInfo> filter)
            {
                this.log = log.ForContext<DnsMonitor>();
                this.filter = filter ?? (_ => true);
            }

            public void OnNext(DnsLookupInfo lookupInfo)
            {
                if (!filter(lookupInfo))
                    return;

                log.Info(
                    "Dns lookup occured with duration: {LookupDuration}. " +
                    "Successfully: {Successfully}",
                    new
                    {
                        LookupDuration = lookupInfo.Latency.ToPrettyString(),
                        Successfully = !lookupInfo.IsFailed
                    });
            }

            public void OnError(Exception error)
                => log.Warn(error);

            public void OnCompleted()
            {
            }
        }
    }
}