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

            if (networkMaxMBitsPerSecond > 0)
            {
                metrics.NetworkInUtilizedPercent = ((double) metrics.NetworkReceivedBytesPerSecond * 8 / (1000 * 1000 * networkMaxMBitsPerSecond) * 100).Clamp(0, 100);
                metrics.NetworkOutUtilizedPercent = ((double) metrics.NetworkSentBytesPerSecond * 8 / (1000 * 1000 * networkMaxMBitsPerSecond) * 100).Clamp(0, 100);
            }
        }
    }
}