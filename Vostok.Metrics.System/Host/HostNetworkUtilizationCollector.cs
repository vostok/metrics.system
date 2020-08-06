using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class HostNetworkUtilizationCollector
    {
        private readonly DerivativeCollector receivedBytesPerSecondCollector;
        private readonly DerivativeCollector sentBytesPerSecondCollector;

        public HostNetworkUtilizationCollector()
        {
            receivedBytesPerSecondCollector = new DerivativeCollector();
            sentBytesPerSecondCollector = new DerivativeCollector();
        }

        public void Collect(HostMetrics metrics, long newReceivedBytesValue, long newSentBytesValue)
        {
            metrics.NetworkReceivedBytesPerSecond = receivedBytesPerSecondCollector.Collect(newReceivedBytesValue);
            metrics.NetworkSentBytesPerSecond = sentBytesPerSecondCollector.Collect(newSentBytesValue);
        }
    }
}