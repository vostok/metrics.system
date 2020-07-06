using JetBrains.Annotations;

namespace Vostok.Metrics.System.GC
{
    // (iloktionov): Do not reorder members, numeric values are important for conversion.

    [PublicAPI]
    public enum GarbageCollectionReason
    {
        AllocSmall,
        Induced,
        LowMemory,
        Empty,
        AllocLarge,
        OutOfSpaceSOH,
        OutOfSpaceLOH,
        InducedNotForced,
        Internal,
        InducedLowMemory,
        InducedCompacting,
        LowMemoryHost,
        PMFullGC
    }
}
