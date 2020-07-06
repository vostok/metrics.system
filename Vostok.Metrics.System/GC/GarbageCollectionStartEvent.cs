using System;

namespace Vostok.Metrics.System.GC
{
    internal class GarbageCollectionStartEvent
    {
        public GarbageCollectionStartEvent(
            DateTime timestamp, 
            int number, 
            int generation, 
            GarbageCollectionType type, 
            GarbageCollectionReason reason)
        {
            Timestamp = timestamp;
            Number = number;
            Generation = generation;
            Type = type;
            Reason = reason;
        }

        public DateTime Timestamp { get; }

        public int Number { get; }

        public int Generation { get; }

        public GarbageCollectionType Type { get; }

        public GarbageCollectionReason Reason { get; }
    }
}
