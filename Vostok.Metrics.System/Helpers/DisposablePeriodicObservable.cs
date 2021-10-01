using System;

namespace Vostok.Metrics.System.Helpers
{
    public class DisposablePeriodicObservable<T> : IObservable<T>, IDisposable
    {
        private readonly PeriodicObservable<T> periodicObservable;
        private readonly Action onDispose;

        public DisposablePeriodicObservable(TimeSpan period, Func<T> factory, Action onDispose)
        {
            periodicObservable = new PeriodicObservable<T>(period, factory);
            this.onDispose = onDispose;
        }

        public IDisposable Subscribe(IObserver<T> observer) => periodicObservable.Subscribe(observer);

        public void Dispose()
        {
            onDispose();
        }
    }
}