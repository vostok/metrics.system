namespace Vostok.Metrics.System.Helpers.Linux;

internal struct ProcSelfStatm
{
    public long PrivateRss; //refers to physical ram
    public long DataSize;
}