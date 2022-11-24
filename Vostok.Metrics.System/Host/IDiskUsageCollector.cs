using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.Host
{
    [PublicAPI]
    public interface IDiskUsageCollector : IDisposable
    {
        /// <summary>
        /// Provides <see cref="DiskUsageInfo"/> for all disks in system.
        /// </summary>
        Dictionary<string, DiskUsageInfo> Collect();
    }
}