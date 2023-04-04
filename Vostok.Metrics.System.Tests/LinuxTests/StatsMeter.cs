using System;
using System.Diagnostics;

namespace Vostok.Metrics.System.Tests.LinuxTests;

internal class StatsMeter
{
    private Stopwatch watch;
    private long startbytes;

    static StatsMeter()
    {
        try
        {
            AppDomain.MonitoringIsEnabled = true; //hack for net framework
        }
        catch //fail on netcoreapp2.1
        {
            Console.WriteLine("GC Meter is OFF");
        }
    }

    public StatsMeter()
    {
        Start();
    }

    public TimeSpan Elapsed => watch.Elapsed;

    public void Start()
    {
        watch = Stopwatch.StartNew();
        startbytes = GetGC();
    }

    public void Print(int iterations)
    {
        var elapsed = Elapsed;
        var garbage = GetGC() - startbytes;
        Console.WriteLine($"{100.0 * elapsed.Ticks / iterations:F0} ns/iteration. Garbage {1.0 * garbage / iterations:F1} bytes/iteration");
        Console.WriteLine($"Total iterations: {iterations}");
    }

    private long GetGC()
    {
        //method simpified. see BancmarkDotNet GcStats.cs
        try
        {
            return AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        }
        catch
        {
            return 0; //fail on netcoreapp2.1
        }
    }
}