using NUnit.Framework;
using Vostok.Commons.Environment;

namespace Vostok.Metrics.System.Tests.Helpers
{
    internal static class RuntimeIgnore
    {
        public static void IgnoreIfIsNotDotNet50AndNewer()
        {
            if (!RuntimeDetector.IsDotNet50AndNewer)
                Assert.Ignore("Only supported on net5.0 and newer");
        }
    }
}