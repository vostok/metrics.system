namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcStat
{
    //note all times are sum across all cores (also true for IdleTime)
    public ulong IdleTime;
    public ulong UserTime;
    public ulong SystemTime;
    public ulong NicedTime;

    public ulong GetTotalTime() => NicedTime + UserTime + SystemTime + IdleTime;
}