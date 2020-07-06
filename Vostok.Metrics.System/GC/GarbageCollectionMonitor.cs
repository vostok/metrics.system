using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Observable;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.GC
{
    /// <summary>
    /// <para>...</para>
    /// <para>...</para>
    /// <para>...</para>
    /// <para>...</para>
    /// <para>...</para>
    /// </summary>
    [PublicAPI]
    public class GarbageCollectionMonitor : EventListener, IObservable<GarbageCollectionInfo>
    {
        private const string GCEventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const int GCKeyword = 0x0000001;
        private const int GCStartEventId = 1;
        private const int GCEndEventId = 2;

        private static readonly Func<EventWrittenEventArgs, DateTime> TimestampProvider
            = ReflectionHelper.BuildInstancePropertyAccessor<EventWrittenEventArgs, DateTime>("TimeStamp");

        private readonly CircularBuffer<GarbageCollectionStartEvent> startEvents 
            = new CircularBuffer<GarbageCollectionStartEvent>(32);

        private readonly BroadcastObservable<GarbageCollectionInfo> observable
            = new BroadcastObservable<GarbageCollectionInfo>();

        private readonly object eventHandlingLock = new object();

        private volatile EventSource gcEventSource;

        public IDisposable Subscribe(IObserver<GarbageCollectionInfo> observer)
            => observable.Subscribe(observer);

        public override void Dispose()
        {
            if (gcEventSource != null)
                DisableEvents(gcEventSource);

            base.Dispose();
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == GCEventSourceName)
                EnableEvents(gcEventSource = source, EventLevel.Informational, (EventKeywords)GCKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs @event)
        {
            try
            {
                lock (eventHandlingLock)
                {
                    switch (@event.EventId)
                    {
                        case GCStartEventId:
                            OnGCStart(@event);
                            break;

                        case GCEndEventId:
                            OnGCEnd(@event);
                            break;
                    }
                }
            }
            catch (Exception error)
            {
                LogProvider.Get().ForContext<GarbageCollectionMonitor>().Warn(error);
            }
        }

        private void OnGCStart(EventWrittenEventArgs @event)
        {
            var timestamp = TimestampProvider(@event);
            if (timestamp == default)
                return;

            var startEvent = new GarbageCollectionStartEvent(
                timestamp,
                (int) GetFieldValue<uint>(@event, "Count"),
                (int) GetFieldValue<uint>(@event, "Depth"),
                (GarbageCollectionType) GetFieldValue<uint>(@event, "Type"),
                (GarbageCollectionReason) GetFieldValue<uint>(@event, "Reason"));

            startEvents.Add(startEvent);
        }

        private void OnGCEnd(EventWrittenEventArgs @event)
        {
            var timestamp = TimestampProvider(@event);
            if (timestamp == default)
                return;

            var endEvent = new GarbageCollectionEndEvent(
                timestamp,
                (int) GetFieldValue<uint>(@event, "Count"),
                (int) GetFieldValue<uint>(@event, "Depth"));

            foreach (var startEvent in startEvents.EnumerateReverse())
            {
                if (Correspond(startEvent, endEvent))
                    ReportCollectionInfo(startEvent, endEvent);
            }
        }

        private void ReportCollectionInfo(GarbageCollectionStartEvent start, GarbageCollectionEndEvent end)
        {
            var info = new GarbageCollectionInfo(
                new DateTimeOffset(start.Timestamp.ToLocalTime()), 
                end.Timestamp - start.Timestamp,
                start.Generation,
                start.Number,
                start.Type,
                start.Reason);

            Task.Run(() => observable.Push(info));
        }

        private bool Correspond(GarbageCollectionStartEvent start, GarbageCollectionEndEvent end)
            => start.Generation == end.Generation && start.Number == end.Number;

        private T GetFieldValue<T>(EventWrittenEventArgs @event, string name)
        {
            if (@event.Payload == null || @event.PayloadNames == null)
                return default;

            var index = @event.PayloadNames.IndexOf(name);
            if (index < 0)
                return default;

            if (@event.Payload[index] is T typedValue)
                return typedValue;

            return default;
        }
    }
}
