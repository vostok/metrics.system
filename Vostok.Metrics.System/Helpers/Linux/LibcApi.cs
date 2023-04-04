using System;
using System.Runtime.InteropServices;

namespace Vostok.Metrics.System.Helpers.Linux;

internal class LibcApi
{
    private const string libc = "libc.so.6";

    [DllImport(libc, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern IntPtr sysconf(int name);
    
    public const int _SC_NPROCESSORS_ONLN = 84;
    public const int _SC_PAGESIZE  = 11;

}