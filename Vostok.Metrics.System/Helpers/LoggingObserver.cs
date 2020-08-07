using System;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Helpers
{
    internal class LoggingObserver<T> : IObserver<T>
    {
        private readonly ILog log;
        private readonly TimeSpan period;
        private readonly Action<ILog, TimeSpan, T> loggingRule;

        public LoggingObserver(ILog log, TimeSpan period, Action<ILog, TimeSpan, T> loggingRule)
        {
            this.period = period;
            this.log = log.ForContext<T>();
            this.loggingRule = loggingRule;
        }

        public void OnNext(T metrics) => loggingRule(log, period, metrics);

        public void OnError(Exception error)
            => log.Warn(error);

        public void OnCompleted() { }
    }
}