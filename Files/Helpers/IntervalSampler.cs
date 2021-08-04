using System;
using System.Diagnostics;

namespace Files.Helpers
{
    internal class IntervalSampler
    {
        private long sampleInterval;
        private Stopwatch stopwatch;

        public IntervalSampler(int millisecondsInterval)
        {
            sampleInterval = millisecondsInterval;
            stopwatch = Stopwatch.StartNew();
        }

        public IntervalSampler(TimeSpan interval)
        {
            sampleInterval = interval.Milliseconds;
            stopwatch = Stopwatch.StartNew();
        }

        public void Reset()
        {
            stopwatch.Restart();
        }

        public bool CheckNow()
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds < sampleInterval)
            {
                return false;
            }

            stopwatch.Restart();
            return true;
        }
    }
}