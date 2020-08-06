using System;
using System.Diagnostics;

namespace Vostok.Metrics.System.Helpers
{
    internal class DerivativeCollector
    {
        private readonly DeltaCollector deltaCollector;
        private readonly Stopwatch stopwatch = new Stopwatch();

        private long currentValue;

        public DerivativeCollector()
        {
            deltaCollector = new DeltaCollector(() => currentValue);
        }

        public double Collect(long newValue)
        {
            currentValue = newValue;
            
            var delta = deltaCollector.Collect();
            var deltaTime = stopwatch.ElapsedTicks / (double) Stopwatch.Frequency;

            stopwatch.Restart();

            return delta <= 0 || deltaTime <= 0
                ? 0
                : delta / deltaTime;
        }
    }
}