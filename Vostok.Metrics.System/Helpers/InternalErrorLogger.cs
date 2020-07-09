using System;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Helpers
{
    internal static class InternalErrorLogger
    {
        private const string SourceContext = "Vostok.Metrics.System";

        public static void Warn(Exception error)
            => LogProvider.Get().ForContext(SourceContext).Warn(error);
    }
}
