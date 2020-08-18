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

        public void Collect(HostMetrics metrics, long newReceivedBytesValue, long newSentBytesValue, long networkMaxMBitsPerSecond)
        {
            metrics.NetworkReceivedBytesPerSecond = (long) receivedBytesPerSecondCollector.Collect(newReceivedBytesValue);
            metrics.NetworkSentBytesPerSecond = (long) sentBytesPerSecondCollector.Collect(newSentBytesValue);
            metrics.NetworkInUtilizedFraction = (double) metrics.NetworkReceivedBytesPerSecond * 8 / (1024 * 1024 * networkMaxMBitsPerSecond) * 100;
            metrics.NetworkOutUtilizedFraction = (double) metrics.NetworkSentBytesPerSecond * 8 / (1024 * 1024 * networkMaxMBitsPerSecond) * 100;
        }
    }
}