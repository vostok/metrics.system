namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcMemInfo
{
    public long AvailableMemory;
    public long KernelMemory;
    public long CacheMemory;
    public long FreeMemory;
    public long TotalMemory;
}