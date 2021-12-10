using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Metrics.System.Dns;
using Vostok.Metrics.System.Helpers;
using Vostok.Metrics.System.Host;

namespace Vostok.Metrics.System.Process
{
    /// <summary>
    /// <para><see cref="CurrentProcessMetricsCollector"/> collects and returns <see cref="CurrentProcessMetrics"/>.</para>
    /// <para>It is not thread-safe and is designed to be invoked periodically without concurrency.</para>
    /// </summary>
    [PublicAPI]
    public class CurrentProcessMetricsCollector : IDisposable
    {
        private static readonly Stopwatch UptimeMeter = Stopwatch.StartNew();

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

        private readonly CurrentProcessMetricsSettings settings;
        private readonly Action<CurrentProcessMetrics> nativeCollector;
        private readonly Action disposeNativeCollector;

        private readonly CurrentProcessSocketMonitor socketMonitor = new CurrentProcessSocketMonitor();

        private readonly DnsMonitor dnsMonitor = new DnsMonitor();
        private readonly CurrentProcessDnsObserver dnsObserver;

        private readonly DeltaCollector lockContentionCount = new DeltaCollector(LockContentionCountProvider);
        private readonly DeltaCollector exceptionsCount = new DeltaCollector(() => ExceptionsCountProvider());
        private readonly DeltaCollector allocatedBytes = new DeltaCollector(() => TotalAllocatedBytesProvider(false));
        private readonly DeltaCollector gen0Collections = new DeltaCollector(() => GcCollectionCountProvider(0));
        private readonly DeltaCollector gen1Collections = new DeltaCollector(() => GcCollectionCountProvider(1));
        private readonly DeltaCollector gen2Collections = new DeltaCollector(() => GcCollectionCountProvider(2));

        private readonly Lazy<long> totalHostMemory = new Lazy<long>(
            () =>
            {
                var settings = HostMetricsSettings.CreateDisabled();

                settings.CollectMemoryMetrics = true;

                using (var collector = new HostMetricsCollector(settings))
                    return collector.Collect().MemoryTotal;
            });

        public void Dispose()
        {
            disposeNativeCollector?.Invoke();
            socketMonitor.Dispose();
            dnsMonitor.Dispose();
        }

        public CurrentProcessMetricsCollector()
            : this(null)
        {
        }

        public CurrentProcessMetricsCollector(CurrentProcessMetricsSettings settings)
        {
            this.settings = settings ?? new CurrentProcessMetricsSettings();

            dnsObserver = new CurrentProcessDnsObserver();
            dnsMonitor.Subscribe(dnsObserver);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                nativeCollector = new NativeMetricsCollector_Windows().Collect;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var collector = new NativeMetricsCollector_Linux();
                nativeCollector = collector.Collect;
                disposeNativeCollector = collector.Dispose;
            }
        }

        [NotNull]
        public CurrentProcessMetrics Collect()
        {
            var metrics = new CurrentProcessMetrics();

            CollectThreadPoolMetrics(metrics);

            CollectGCMetrics(metrics);

            CollectMiscMetrics(metrics);

            CollectNativeMetrics(metrics);

            CollectLimitsMetrics(metrics);

            CollectSocketMetrics(metrics);

            CollectDnsMetrics(metrics);

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

            if (metrics.ThreadPoolMinWorkers > 0)
                metrics.ThreadPoolWorkersUtilizedFraction = ((double)metrics.ThreadPoolBusyWorkers / metrics.ThreadPoolMinWorkers).Clamp(0, 1);

            if (metrics.ThreadPoolMinIo > 0)
                metrics.ThreadPoolIoUtilizedFraction = ((double)metrics.ThreadPoolBusyIo / metrics.ThreadPoolMinIo).Clamp(0, 1);
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
            metrics.UptimeSeconds = UptimeMeter.Elapsed.TotalSeconds;
        }

        private void CollectNativeMetrics(CurrentProcessMetrics metrics)
            => nativeCollector?.Invoke(metrics);

        private void CollectLimitsMetrics(CurrentProcessMetrics metrics)
        {
            var cpuCoresLimit = settings.CpuCoresLimitProvider?.Invoke();
            metrics.CpuLimitCores = cpuCoresLimit ?? Environment.ProcessorCount;
            metrics.HasCpuLimit = cpuCoresLimit != null;

            if (metrics.CpuLimitCores > 0)
                metrics.CpuUtilizedFraction = (metrics.CpuUtilizedCores / metrics.CpuLimitCores).Clamp(0, 1);

            var memoryBytesLimit = settings.MemoryBytesLimitProvider?.Invoke();
            metrics.MemoryLimit = memoryBytesLimit ?? totalHostMemory.Value;
            metrics.HasMemoryLimit = memoryBytesLimit != null;

            if (metrics.MemoryLimit > 0)
                metrics.MemoryUtilizedFraction = ((double) metrics.MemoryResident / metrics.MemoryLimit).Clamp(0, 1);
        }

        private void CollectSocketMetrics(CurrentProcessMetrics metrics) =>
            socketMonitor.Collect(metrics);

        private void CollectDnsMetrics(CurrentProcessMetrics metrics) =>
            dnsObserver.Collect(metrics);
    }
}