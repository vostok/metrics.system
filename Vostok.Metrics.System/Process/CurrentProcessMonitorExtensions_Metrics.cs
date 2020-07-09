using System;
using JetBrains.Annotations;
using Vostok.Metrics.Models;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public static class CurrentProcessMonitorExtensions_Metrics
    {
        /// <summary>
        /// <para>Enables reporting of system metrics of the current process.</para>
        /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
        /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
        /// </summary>
        [NotNull]
        public static IDisposable ReportMetrics([NotNull] this CurrentProcessMonitor monitor, [NotNull] IMetricContext metricContext, TimeSpan period)
            => monitor.ObserveMetrics(period).Subscribe(new ReportingObserver(metricContext));

        private class ReportingObserver : IObserver<CurrentProcessMetrics>
        {
            private readonly IMetricContext metricContext;

            public ReportingObserver(IMetricContext metricContext)
                => this.metricContext = metricContext;

            public void OnNext(CurrentProcessMetrics value)
            {
                foreach (var property in typeof(CurrentProcessMetrics).GetProperties())
                    Send(property.Name, (double)property.GetValue(value));
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }

            private void Send(string name, double value)
                => metricContext.Send(new MetricDataPoint(value, (WellKnownTagKeys.Name, name)));
        }
    }
}
