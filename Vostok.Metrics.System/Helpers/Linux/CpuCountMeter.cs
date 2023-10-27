using System;

namespace Vostok.Metrics.System.Helpers.Linux;

internal class CpuCountMeter
{
    private readonly bool useDotnetCpuCount;

    public CpuCountMeter(bool useDotnetCpuCount)
    {
        this.useDotnetCpuCount = useDotnetCpuCount;
    }

    /// <summary>
    /// Count ONLINE cpu's for system
    /// docker container behaviour = should return system cores count, not container limits 
    /// </summary>
    public int GetCpuCount()
    {
        try
        {
            if (useDotnetCpuCount)
                return Environment.ProcessorCount; //in .net, see [utils.cpp int GetCurrentProcessCpuCount()]. can read from config, or (linux) calc some magic from cgroups

            //NOTE reading '/proc/cpuinfo' provides same results (only ONLINE cpus fox x86) arch\x86\kernel\cpu\proc.c
            //file /proc/stat also have only online cpus
            //note this count can be changed (eg via 'chcpu' command) - can provide spikes in cpu usage measurments if use CpuCount
            var number = LibcApi.sysconf(LibcApi._SC_NPROCESSORS_ONLN);
            if ((long)number < 0)
                return Environment.ProcessorCount; //note this is function in Linux. (maybe also calls sysconf. can read env var??)
            return (int)number;
        }
        catch
        {
            return 1; //safe value.
        }
    }
}