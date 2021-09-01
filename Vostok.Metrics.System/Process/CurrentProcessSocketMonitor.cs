using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Process
{
    internal class CurrentProcessSocketMonitor : EventListener
    {
        private const string SourceName = "System.Net.Sockets";

        // NOTE: We check certain events in System.Net.Sockets event source.
        // NOTE: See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SocketsTelemetry.cs for details.

        #region EventId

        private const int ConnectStartEventId = 1;
        private const int AcceptStartEventId = 4;
        private const int ConnectFailedEventId = 3;
        private const int AcceptFailedEventId = 6;

        private const int CounterEventId = -1;

        private const string ReceivedDatagramsCounterName = "datagrams-received";
        private const string SentDatagramsCounterName = "datagrams-sent";

        #endregion

        private readonly ConcurrentCounter outgoingTcpConnectionsCounter = new ConcurrentCounter();
        private readonly ConcurrentCounter incomingTcpConnectionsCounter = new ConcurrentCounter();
        private readonly ConcurrentCounter failedTcpConnectionsCounter = new ConcurrentCounter();

        private readonly DeltaCollector outgoingDatagramsCounter;
        private readonly DeltaCollector incomingDatagramsCounter;
        private long outgoingDatagrams;
        private long incomingDatagrams;

        public CurrentProcessSocketMonitor()
        {
            outgoingDatagramsCounter = new DeltaCollector(() => outgoingDatagrams);
            incomingDatagramsCounter = new DeltaCollector(() => incomingDatagrams);
        }

        public void Collect(CurrentProcessMetrics metrics)
        {
            metrics.OutgoingTcpConnectionsCount = outgoingTcpConnectionsCounter.CollectAndReset();
            metrics.IncomingTcpConnectionsCount = incomingTcpConnectionsCounter.CollectAndReset();
            metrics.FailedTcpConnectionsCount = failedTcpConnectionsCounter.CollectAndReset();

            metrics.OutgoingDatagramsCount = outgoingDatagramsCounter.Collect();
            metrics.IncomingDatagramsCount = incomingDatagramsCounter.Collect();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == SourceName)
                EnableEvents(eventSource,
                    EventLevel.Verbose,
                    EventKeywords.All,
                    new Dictionary<string, string>
                    {
                        {"EventCounterIntervalSec", "0.1"}
                    });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var id = eventData.EventId;

            if (IsCounter(id))
                CollectCounterValue(eventData);

            if (IsIncoming(id))
                incomingTcpConnectionsCounter.Increment();
            if (IsOutgoing(id))
                outgoingTcpConnectionsCounter.Increment();
            if (IsFailed(id))
                failedTcpConnectionsCounter.Increment();
        }

        private void CollectCounterValue(EventWrittenEventArgs eventData)
        {
            if (TryGetCounterValue(eventData, SentDatagramsCounterName, out var value))
                Interlocked.Exchange(ref outgoingDatagrams, value);

            if (TryGetCounterValue(eventData, ReceivedDatagramsCounterName, out value))
                Interlocked.Exchange(ref incomingDatagrams, value);
        }

        private static bool TryGetCounterValue(EventWrittenEventArgs eventData, string counterName, out long value)
        {
            value = 0;
            if (eventData.Payload?.Count <= 0
                || !(eventData.Payload?[0] is IDictionary<string, object> data)
                || !data.TryGetValue("Name", out var n)
                || !(n is string name)
                || name != counterName) return false;

            if (!data.TryGetValue("Mean", out var mean))
                return false;
            value = Convert.ToInt64(mean);
            return true;
        }

        private static bool IsIncoming(int eventId) =>
            eventId == AcceptStartEventId;

        private static bool IsOutgoing(int eventId) =>
            eventId == ConnectStartEventId;

        private static bool IsFailed(int eventId) =>
            eventId == ConnectFailedEventId || eventId == AcceptFailedEventId;

        private static bool IsCounter(int eventId) =>
            eventId == CounterEventId;
    }
}