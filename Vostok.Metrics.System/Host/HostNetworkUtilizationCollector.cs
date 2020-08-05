using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.System.Host
{
    internal class HostNetworkUtilizationCollector
    {
        private readonly DerivativeCollector receivedBytesPerSecondCollector;
        private readonly DerivativeCollector sentBytesPerSecondCollector;

        private long currentReceivedBytesValue;
        private long currentSentBytesValue;

        public HostNetworkUtilizationCollector()
        {
            receivedBytesPerSecondCollector = new DerivativeCollector(() => currentReceivedBytesValue);
            sentBytesPerSecondCollector = new DerivativeCollector(() => currentSentBytesValue);
        }

        public void Collect(HostMetrics metrics, long newReceivedBytesValue, long newSentBytesValue)
        {
            currentReceivedBytesValue = newReceivedBytesValue;
            currentSentBytesValue = newSentBytesValue;

            metrics.NetworkReceivedBytesPerSecond = receivedBytesPerSecondCollector.Collect();
            metrics.NetworkSentBytesPerSecond = sentBytesPerSecondCollector.Collect();
        }
    }
}