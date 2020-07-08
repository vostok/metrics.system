using JetBrains.Annotations;

namespace Vostok.Metrics.System.Gc
{
    // (iloktionov): Do not reorder members, numeric values are important for conversion.

    [PublicAPI]
    public enum GarbageCollectionType
    {
        NonConcurrentGC,
        BackgroundGC,
        ForegroundGC
    }
}
