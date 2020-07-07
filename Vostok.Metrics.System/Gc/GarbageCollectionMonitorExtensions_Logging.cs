using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Gc
{
    [PublicAPI]
    public static class GarbageCollectionMonitorExtensions_Logging
    {
        [NotNull]
        public static IDisposable LogCollections([NotNull] this GarbageCollectionMonitor monitor, [NotNull] ILog log, [CanBeNull] Predicate<GarbageCollectionInfo> filter)
            => monitor.Subscribe(new LoggingObserver(log, filter));

        private class LoggingObserver : IObserver<GarbageCollectionInfo>
        {
            private readonly ILog log;
            private readonly Predicate<GarbageCollectionInfo> filter;

            public LoggingObserver(ILog log, Predicate<GarbageCollectionInfo> filter)
            {
                this.log = log.ForContext("GCMonitor");
                this.filter = filter ?? (_ => true);
            }

            public void OnNext(GarbageCollectionInfo gc)
            {
                if (!filter(gc))
                    return;

                log.Info(
                    "Gen-{GcGeneration} GC occured with duration = {GcDurationPretty}. " +
                    "Type = {GcType}. Reason = {GcReason}. Number = {GcNumber}. " +
                    "Started at {GcStartTimestamp:O}. Ended at {GcEndTimestamp:O}.",
                    new
                    {
                        GcGeneration = gc.Generation,
                        GcDurationPretty = gc.Duration.ToPrettyString(),
                        GcDurationMs = gc.Duration.TotalMilliseconds,
                        GcType = gc.Type,
                        GcReason = gc.Reason,
                        GcNumber = gc.Number,
                        GcStartTimestamp = gc.StartTimestamp,
                        GcEndTimestamp = gc.StartTimestamp + gc.Duration
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
