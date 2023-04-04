namespace Vostok.Metrics.System.Process;

public class LinuxProcessMetricsSettings
{
    /// <summary>
    /// Count if open file descriptions (via /proc/self/fd/) is very slow, and takes VERY long time if mulltions of files opened...
    /// There is no way to calculate this count faster
    /// </summary>
    public bool DisableOpenFilesCount { get; set; }
    
    /// <summary>
    /// use Environment.ProcessorCount for cpu count
    /// </summary>
    public bool UseDotnetCpuCount { get; set; }

    public bool DisableCgroupStats { get; set; }
}