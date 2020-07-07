using System;
using JetBrains.Annotations;
using Vostok.Metrics.Grouping;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.System.Gc
{
    [PublicAPI]
    public static class GarbageCollectionMonitorExtensions_Metrics
    {
        [NotNull]
        public static IDisposable MeasureCollections([NotNull] this GarbageCollectionMonitor monitor, [NotNull] IMetricContext metricContext)
            => monitor.Subscribe(new MeasuringObserver(metricContext));

        private class MeasuringObserver : IObserver<GarbageCollectionInfo>
        {
            private static readonly FloatingGaugeConfig config = new FloatingGaugeConfig
            {
                ResetOnScrape = true,
                Unit = WellKnownUnits.Milliseconds
            };

            private readonly IMetricGroup1<IFloatingGauge> gcTotalDuration;
            private readonly IMetricGroup1<IFloatingGauge> gcLongestDuration;

            public MeasuringObserver(IMetricContext metricContext)
            {
                gcTotalDuration = metricContext.CreateFloatingGauge("gcTotalDurationMs", "gcType", config);
                gcLongestDuration = metricContext.CreateFloatingGauge("gcLongestDurationMs", "gcType", config);
            }

            public void OnNext(GarbageCollectionInfo value)
            {
                gcTotalDuration.For(value.Type).Add(value.Duration.TotalMilliseconds);
                gcTotalDuration.For("All").Add(value.Duration.TotalMilliseconds);

                gcLongestDuration.For(value.Type).TryIncreaseTo(value.Duration.TotalMilliseconds);
                gcLongestDuration.For("All").TryIncreaseTo(value.Duration.TotalMilliseconds);
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}
