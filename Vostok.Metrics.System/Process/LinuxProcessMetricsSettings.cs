namespace Vostok.Metrics.System.Process;

public class LinuxProcessMetricsSettings
{
    /// <summary>
    /// Count if open file descriptions (via /proc/self/fd/) is very slow, and takes VERY long time if millions of files opened...
    /// There is no way to calculate this count faster
    /// </summary>
    public bool DisableOpenFilesCount { get; set; }

    /// <summary>
    /// use Environment.ProcessorCount for available cpu count for current process metrics
    /// </summary>
    public bool UseDotnetCpuCount { get; set; }

    /// <summary>
    /// dont fill cgroup stats. may speed up metrics collection
    /// </summary>
    public bool DisableCgroupStats { get; set; }
}