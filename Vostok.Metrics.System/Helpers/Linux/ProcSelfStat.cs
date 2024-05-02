namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcSelfStat
{
    //note process stat does not count nice time(it's included in utime)
    public ulong utime;
    public ulong stime;
}