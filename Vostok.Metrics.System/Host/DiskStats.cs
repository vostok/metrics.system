namespace Vostok.Metrics.System.Host;

internal class DiskStats
{
    public string DiskName { get; set; }
    public long ReadsCount { get; set; }
    public long SectorsReadCount { get; set; }
    public long SectorsWrittenCount { get; set; }
    public long WritesCount { get; set; }
    public long MsSpentReading { get; set; }
    public long MsSpentWriting { get; set; }
    public long CurrentQueueLength { get; set; }
    public long MsSpentDoingIo { get; set; }
}