using System;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    // TODO(iloktionov): benchmark
    // TODO(iloktionov): add logging extensions
    // TODO(iloktionov): add metrics extensions
    // TODO(iloktionov): add unit tests
    // TODO(iloktionov): refactor error logging across the board

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

        private long prevLockContentionCount;
        private long prevTotalAllocatedBytes;
        private int prevExceptionsCount;
        private int prevGen0Collections;
        private int prevGen1Collections;
        private int prevGen2Collections;

        public CurrentProcessMetricsCollector()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                nativeCollector = new WindowsNativeMetricsCollector().Collect;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                nativeCollector = new LinuxNativeMetricsCollector().Collect;
        }

        [NotNull]
        public CurrentProcessMetrics Collect()
        {
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minIocpThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIocpThreads);
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIocpThreads);

            var allocatedBytes = TotalAllocatedBytesProvider(false) - prevTotalAllocatedBytes;
            var gen0Collections = GcCollectionCountProvider(0) - prevGen0Collections;
            var gen1Collections = GcCollectionCountProvider(1) - prevGen1Collections;
            var gen2Collections = GcCollectionCountProvider(2) - prevGen2Collections;
            var exceptionsCount = (int) (ExceptionsCountProvider() - prevExceptionsCount);
            var lockContentionCount = (int) (LockContentionCountProvider() - prevLockContentionCount);

            Interlocked.Add(ref prevLockContentionCount, lockContentionCount);
            Interlocked.Add(ref prevTotalAllocatedBytes, allocatedBytes);
            Interlocked.Add(ref prevExceptionsCount, exceptionsCount);
            Interlocked.Add(ref prevGen0Collections, gen0Collections);
            Interlocked.Add(ref prevGen1Collections, gen1Collections);
            Interlocked.Add(ref prevGen2Collections, gen2Collections);

            var metrics = new CurrentProcessMetrics
            {
                GcHeapSize = GC.GetTotalMemory(false),
                GcGen0Size = (long) GcGenerationSizeProvider(0),
                GcGen1Size = (long) GcGenerationSizeProvider(1),
                GcGen2Size = (long) GcGenerationSizeProvider(2),
                GcLOHSize = (long) GcGenerationSizeProvider(3),
                GcAllocatedBytes = allocatedBytes,
                GcGen0Collections = gen0Collections,
                GcGen1Collections = gen1Collections,
                GcGen2Collections = gen2Collections,
                GcTimePercent = GcTimeLastPercentProvider(),
                ThreadPoolTotalCount = ThreadPoolTotalCountProvider(),
                ThreadPoolMinWorkers = minWorkerThreads,
                ThreadPoolMinIocp = minIocpThreads,
                ThreadPoolBusyWorkers = maxWorkerThreads - availableWorkerThreads,
                ThreadPoolBusyIocp = maxIocpThreads - availableIocpThreads,
                ThreadPoolQueueLength = ThreadPoolQueueLengthProvider(),
                LockContentionCount = lockContentionCount,
                ExceptionsCount = exceptionsCount,
                ActiveTimersCount = ActiveTimersCountProvider(),
            };

            nativeCollector?.Invoke(metrics);

            return metrics;
        }
    }
}
