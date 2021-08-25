using System.Diagnostics.Tracing;
using System.Threading;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    internal class CurrentProcessSocketMonitor : EventListener
    {
        private const string SourceName = "System.Net.Sockets";

        private readonly DeltaCollector outgoingConnections;
        private readonly DeltaCollector incomingConnections;
        private readonly DeltaCollector failedConnections;
        
        private long outgoingConnectionsCounter;
        private long incomingConnectionsCounter;
        private long failedConnectionsCounter;

        public CurrentProcessSocketMonitor()
        {
            outgoingConnections = new DeltaCollector(() => outgoingConnectionsCounter);
            incomingConnections = new DeltaCollector(() => incomingConnectionsCounter);
            failedConnections = new DeltaCollector(() => failedConnectionsCounter);
        }

        public void Collect(CurrentProcessMetrics metrics)
        {
            metrics.OutgoingConnectionsCount = outgoingConnections.Collect();
            metrics.IncomingConnectionsCount = incomingConnections.Collect();
            metrics.FailedConnectionsCount = failedConnections.Collect();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == SourceName)
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var id = eventData.EventId;
            
            if (IsIncoming(id))
                Interlocked.Increment(ref incomingConnectionsCounter);
            if (IsOutgoing(id))
                Interlocked.Increment(ref outgoingConnectionsCounter);
            if (IsFailed(id))
                Interlocked.Increment(ref failedConnectionsCounter);
        }

        private static bool IsIncoming(int eventId) =>
            eventId == 4;

        private static bool IsOutgoing(int eventId) =>
            eventId == 1;

        private static bool IsFailed(int eventId) =>
            eventId == 3 || eventId == 6;
    }
}