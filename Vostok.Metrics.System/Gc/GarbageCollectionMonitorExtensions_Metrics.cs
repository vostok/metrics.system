using System;
using JetBrains.Annotations;
using Vostok.Metrics.Grouping;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.System.Gc
{
    [PublicAPI]
    public static class GarbageCollectionMonitorExtensions_Metrics
    {
        /// <summary>
        /// <para>Enables reporting of following GC-based metrics:</para>
        /// <list type="bullet">
        ///     <item><description>Total GC duration in ms (sum of all collections for the reporting period).</description></item>
        ///     <item><description>Total GC duration per type in ms (sum of all collections for the reporting period).</description></item>
        ///     <item><description>Longest GC duration in ms (of all collections for the reporting period).</description></item>
        ///     <item><description>Longest GC duration per type in ms (of all collections for the reporting period).</description></item>
        /// </list>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
        /// </summary>
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
