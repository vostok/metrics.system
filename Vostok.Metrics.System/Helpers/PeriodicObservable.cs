using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Helpers
{
    internal class PeriodicObservable<T> : IObservable<T>
    {
        private readonly TimeSpan period;
        private readonly Func<T> provider;

        private readonly object observersLock = new object();
        private volatile List<IObserver<T>> observers = new List<IObserver<T>>();
        private volatile CancellationTokenSource cancellation = new CancellationTokenSource();

        public PeriodicObservable(TimeSpan period, Func<T> provider)
        {
            this.period = period;
            this.provider = provider;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (observersLock)
            {
                var newObservers = new List<IObserver<T>>(observers.Count + 1);

                newObservers.AddRange(observers);
                newObservers.Add(observer);

                observers = newObservers;

                if (observers.Count == 1)
                    Task.Run(() => ProduceElementsAsync(cancellation.Token));
            }

            return new Subscription(this, observer);
        }

        private async Task ProduceElementsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var watch = Stopwatch.StartNew();

                try
                {
                    var element = provider();

                    foreach (var observer in observers)
                        observer.OnNext(element);
                }
                catch (Exception error)
                {
                    LogProvider.Get().ForContext("Vostok.Metrics.System").Warn(error);
                }

                var remainingWait = period - watch.Elapsed;
                if (remainingWait > TimeSpan.Zero)
                    await Task.Delay(remainingWait, token).ConfigureAwait(false);
            }
        }

        #region Subscription

        private class Subscription : IDisposable
        {
            private readonly PeriodicObservable<T> observable;
            private readonly IObserver<T> observer;

            public Subscription(PeriodicObservable<T> observable, IObserver<T> observer)
            {
                this.observable = observable;
                this.observer = observer;
            }

            public void Dispose()
            {
                lock (observable.observersLock)
                {
                    var newObservers = new List<IObserver<T>>(observable.observers);

                    newObservers.Remove(observer);

                    observable.observers = newObservers;

                    if (newObservers.Count == 0)
                    {
                        observable.cancellation.Cancel();
                        observable.cancellation = new CancellationTokenSource();
                    }
                }
            }
        }

        #endregion
    }
}
