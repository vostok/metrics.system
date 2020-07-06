using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.System.GC
{
    /// <summary>
    /// <see cref="GarbageCollectionInfo"/> describes a single garbage collection that occurred in current process.
    /// </summary>
    [PublicAPI]
    public class GarbageCollectionInfo
    {
        public GarbageCollectionInfo(
            DateTimeOffset startTimestamp,
            TimeSpan duration,
            int generation,
            int number,
            GarbageCollectionType type,
            GarbageCollectionReason reason)
        {
            StartTimestamp = startTimestamp;
            Duration = duration;
            Generation = generation;
            Number = number;
            Type = type;
            Reason = reason;
        }

        /// <summary>
        /// Start time of the garbage collection.
        /// </summary>
        public DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// Total duration of the garbage collection, including nonblocking parts.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Collection generation (from 0 to 2)
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Total number of collections since the application start.
        /// </summary>
        public int Number { get; }

        public GarbageCollectionType Type { get; }

        public GarbageCollectionReason Reason { get; }
    }
}
