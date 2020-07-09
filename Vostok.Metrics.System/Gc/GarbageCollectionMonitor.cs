using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Observable;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Gc
{
    /// <summary>
    /// <para><see cref="GarbageCollectionMonitor"/> emits notifications about garbage collections in current process.</para>
    /// <para>Should only be used on .NET Core 3.0+ due to unavailability of internal CLR tracing events in earlier versions.</para>
    /// <para>Subscribe to receive instances of <see cref="GarbageCollectionInfo"/>.</para>
    /// <para>Remember to dispose of the monitor when it's no longer needed.</para>
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
            = new CircularBuffer<GarbageCollectionStartEvent>(64);

        private readonly BroadcastObservable<GarbageCollectionInfo> observable
            = new BroadcastObservable<GarbageCollectionInfo>();

        public IDisposable Subscribe(IObserver<GarbageCollectionInfo> observer)
            => observable.Subscribe(observer);

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == GCEventSourceName)
                EnableEvents(source, EventLevel.Informational, (EventKeywords)GCKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs @event)
        {
            if (!observable.HasObservers)
                return;

            try
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
            catch (Exception error)
            {
                InternalErrorLogger.Warn(error);
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

            lock (startEvents)
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

            lock (startEvents)
            {
                foreach (var startEvent in startEvents.EnumerateReverse())
                {
                    if (Correspond(startEvent, endEvent))
                        ReportCollectionInfo(startEvent, endEvent);
                }
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
