using System;

namespace Vostok.Metrics.System.Gc
{
    internal class GarbageCollectionEndEvent
    {
        public GarbageCollectionEndEvent(DateTime timestamp, int number, int generation)
        {
            Timestamp = timestamp;
            Number = number;
            Generation = generation;
        }

        public DateTime Timestamp { get; }

        public int Number { get; }

        public int Generation { get; }
    }
}
