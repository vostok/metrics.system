namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcStat
{
    public int CpuCount;
    public ulong IdleTime;
    public ulong UserTime;
    public ulong SystemTime;
    public ulong NicedTime;
}