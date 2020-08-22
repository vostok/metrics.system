using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Metrics.System.Helpers
{
    internal class ThrottlingCache<T>
    {
        private readonly Func<T> provider;
        private readonly TimeSpan ttl;
        private volatile Lazy<T> container;

        public ThrottlingCache(Func<T> provider, TimeSpan ttl)
        {
            this.provider = provider;
            this.ttl = ttl;

            ResetContainer();
        }

        public T Obtain()
            => container.Value;

        private void ResetContainer()
        {
            container = new Lazy<T>(
                () =>
                {
                    try
                    {
                        return provider();
                    }
                    finally
                    {
                        Task.Delay(ttl).ContinueWith(_ => ResetContainer());
                    }
                }, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
