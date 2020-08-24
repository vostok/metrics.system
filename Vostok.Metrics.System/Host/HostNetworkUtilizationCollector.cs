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
                var networkMaxBitsPerSecond = networkMaxMBitsPerSecond * 1000d * 1000d;
                var receivedBitsPerSecond = metrics.NetworkReceivedBytesPerSecond * 8d;
                var sentBitsPerSecond = metrics.NetworkSentBytesPerSecond * 8d;

                metrics.NetworkInUtilizedPercent = (100d * receivedBitsPerSecond / networkMaxBitsPerSecond).Clamp(0, 100);
                metrics.NetworkOutUtilizedPercent = (100d * sentBitsPerSecond / networkMaxBitsPerSecond).Clamp(0, 100);
            }
        }
    }
}