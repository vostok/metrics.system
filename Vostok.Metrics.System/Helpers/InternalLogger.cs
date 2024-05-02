using System;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.System.Helpers
{
    internal static class InternalLogger
    {
        private const string SourceContext = "Vostok.Metrics.System";

        public static void Warn(Exception error)
            => LogProvider.Get().ForContext(SourceContext).Warn(error);
        
        public static void Debug(string context, string message)
            => LogProvider.Get().ForContext(SourceContext).ForContext(context).Debug(message);
    }
}
