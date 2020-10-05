using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Process
{
    [PublicAPI]
    public class CurrentProcessMetricsSettings
    {
        [CanBeNull]
        public Func<double?> CpuCoresLimitProvider { get; set; }

        [CanBeNull]
        public Func<long?> MemoryBytesLimitProvider { get; set; }
    }
}
