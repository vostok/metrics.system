using System;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    /// <summary>
    /// <para><see cref="CurrentProcessMetricsCollector"/> collects and returns <see cref="CurrentProcessMetrics"/>.</para>
    /// <para>It is not thread-safe and is designed to be invoked periodically without concurrency.</para>
    /// </summary>
    [PublicAPI]
    public class CurrentProcessMetricsCollector
    {
        private static readonly Func<int, int> GcCollectionCountProvider
            = ReflectionHelper.BuildStaticMethodInvoker<int, int>(typeof(GC), "CollectionCount");

        private static readonly Func<int, ulong> GcGenerationSizeProvider
            = ReflectionHelper.BuildStaticMethodInvoker<int, ulong>(typeof(GC), "GetGenerationSize");

        private static readonly Func<int> GcTimeLastPercentProvider
            = ReflectionHelper.BuildStaticMethodInvoker<int>(typeof(GC), "GetLastGCPercentTimeInGC");

        private static readonly Func<bool, long> TotalAllocatedBytesProvider
            = ReflectionHelper.BuildStaticMethodInvoker<bool, long>(typeof(GC), "GetTotalAllocatedBytes");

        private static readonly Func<uint> ExceptionsCountProvider
            = ReflectionHelper.BuildStaticMethodInvoker<uint>(typeof(Exception), "GetExceptionCount");

        private static readonly Func<long> ActiveTimersCountProvider
            = ReflectionHelper.BuildStaticPropertyAccessor<long>(typeof(Timer), "ActiveCount");

        private static readonly Func<long> LockContentionCountProvider
            = ReflectionHelper.BuildStaticPropertyAccessor<long>(typeof(Monitor), "LockContentionCount");

        private static readonly Func<int> ThreadPoolTotalCountProvider
            = ReflectionHelper.BuildStaticPropertyAccessor<int>(typeof(ThreadPool), "ThreadCount");

        private static readonly Func<long> ThreadPoolQueueLengthProvider
            = ReflectionHelper.BuildStaticPropertyAccessor<long>(typeof(ThreadPool), "PendingWorkItemCount");

        private readonly Action<CurrentProcessMetrics> nativeCollector;

        private readonly DeltaCollector lockContentionCount = new DeltaCollector(LockContentionCountProvider);
        private readonly DeltaCollector exceptionsCount = new DeltaCollector(() => ExceptionsCountProvider());
        private readonly DeltaCollector allocatedBytes = new DeltaCollector(() => TotalAllocatedBytesProvider(false));
        private readonly DeltaCollector gen0Collections = new DeltaCollector(() => GcCollectionCountProvider(0));
        private readonly DeltaCollector gen1Collections = new DeltaCollector(() => GcCollectionCountProvider(1));
        private readonly DeltaCollector gen2Collections = new DeltaCollector(() => GcCollectionCountProvider(2));

        public CurrentProcessMetricsCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                nativeCollector = new NativeMetricsCollector_Windows().Collect;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                nativeCollector = new NativeMetricsCollector_Linux().Collect;
        }

        [NotNull]
        public CurrentProcessMetrics Collect()
        {
            var metrics = new CurrentProcessMetrics();

            CollectThreadPoolMetrics(metrics);

            CollectGCMetrics(metrics);

            CollectMiscMetrics(metrics);

            CollectNativeMetrics(metrics);
            
            return metrics;
        }

        private void CollectThreadPoolMetrics(CurrentProcessMetrics metrics)
        {
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minIocpThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIocpThreads);
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIocpThreads);

            metrics.ThreadPoolMinWorkers = minWorkerThreads;
            metrics.ThreadPoolMinIo = minIocpThreads;
            metrics.ThreadPoolBusyWorkers = maxWorkerThreads - availableWorkerThreads;
            metrics.ThreadPoolBusyIo = maxIocpThreads - availableIocpThreads;
            metrics.ThreadPoolTotalCount = ThreadPoolTotalCountProvider();
            metrics.ThreadPoolQueueLength = ThreadPoolQueueLengthProvider();
        }

        private void CollectGCMetrics(CurrentProcessMetrics metrics)
        {
            metrics.GcAllocatedBytes = allocatedBytes.Collect();
            metrics.GcGen0Collections = (int) gen0Collections.Collect();
            metrics.GcGen1Collections = (int) gen1Collections.Collect();
            metrics.GcGen2Collections = (int) gen2Collections.Collect();

            metrics.GcHeapSize = GC.GetTotalMemory(false);
            metrics.GcGen0Size = (long) GcGenerationSizeProvider(0);
            metrics.GcGen1Size = (long) GcGenerationSizeProvider(1);
            metrics.GcGen2Size = (long) GcGenerationSizeProvider(2);
            metrics.GcLOHSize = (long) GcGenerationSizeProvider(3);
            metrics.GcTimePercent = GcTimeLastPercentProvider();
        }

        private void CollectMiscMetrics(CurrentProcessMetrics metrics)
        {
            metrics.LockContentionCount = (int) lockContentionCount.Collect();
            metrics.ExceptionsCount = (int) exceptionsCount.Collect();
            metrics.ActiveTimersCount = ActiveTimersCountProvider();
        }

        private void CollectNativeMetrics(CurrentProcessMetrics metrics)
            => nativeCollector?.Invoke(metrics);
    }
}
