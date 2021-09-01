using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Observable;

namespace Vostok.Metrics.System.Dns
{
    /// <summary>
    /// <para><see cref="DnsMonitor"/>Emits notifications about every dns lookup in current process.</para>
    /// <para>Should only be used on .NET Core 5.0+ due to unavailability of DNS events in earlier versions.</para>
    /// <para>Subscribe to receive instances of <see cref="DnsLookupInfo"/>.</para>
    /// <para>Remember to dispose of the monitor when it's no longer needed.</para>
    /// </summary>
    [PublicAPI]
    public class DnsMonitor : EventListener, IObservable<DnsLookupInfo>
    {
        private const string SourceName = "System.Net.NameResolution";

        // NOTE: We check certain events in System.Net.NameResolution event source.
        // NOTE: See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.NameResolution/src/System/Net/NameResolutionTelemetry.cs for details.

        #region EventId

        private const int ResolutionStartEventId = 1;
        private const int ResolutionStopEventId = 2;
        private const int ResolutionFailedEventId = 3;

        #endregion

        // NOTE: Method call for the event is executed in the same synchronization context, so we can track the start and end of the dns lookup
        private readonly AsyncLocal<LookupInfo> lookupStartTime = new AsyncLocal<LookupInfo>();

        private readonly BroadcastObservable<DnsLookupInfo> observable
            = new BroadcastObservable<DnsLookupInfo>();

        public IDisposable Subscribe(IObserver<DnsLookupInfo> observer) =>
            observable.Subscribe(observer);

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (SourceName == eventSource.Name)
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!observable.HasObservers)
                return;
            var id = eventData.EventId;

            switch (id)
            {
                case ResolutionStartEventId:
                    lookupStartTime.Value = new LookupInfo(DateTime.Now);
                    break;
                case ResolutionStopEventId:
                    ReportLookupInfo();
                    break;
                case ResolutionFailedEventId:
                    FailedLookup();
                    break;
            }
        }

        private void ReportLookupInfo()
        {
            var lookupInfo = lookupStartTime.Value;
            if (lookupInfo.StartTime == default)
                return;

            var info = new DnsLookupInfo(lookupInfo.IsFailed, DateTime.Now - lookupInfo.StartTime);
            Task.Run(() => observable.Push(info));
        }

        private void FailedLookup()
        {
            var lookupInfo = lookupStartTime.Value;
            if (lookupInfo.StartTime == default)
                return;
            lookupInfo.IsFailed = true;
            lookupStartTime.Value = lookupInfo;
        }

        private struct LookupInfo
        {
            public readonly DateTime StartTime;
            public bool IsFailed;

            public LookupInfo(DateTime startTime)
            {
                StartTime = startTime;
                IsFailed = false;
            }
        }
    }
}