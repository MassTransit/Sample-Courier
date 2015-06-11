namespace TrackingService
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;


    public class RoutingSlipMetrics
    {
        readonly ConcurrentBag<TimeSpan> _durations;
        long _completedCount;
        readonly string _description;

        public RoutingSlipMetrics(string description)
        {
            _description = description;
            _completedCount = 0;
            _durations = new ConcurrentBag<TimeSpan>();
        }

        public void AddComplete(TimeSpan duration)
        {
            long count = Interlocked.Increment(ref _completedCount);
            _durations.Add(duration);

            if (count % 100 == 0)
                Snapshot();
        }

        public void Snapshot()
        {
            TimeSpan[] snapshot = _durations.ToArray();
            double averageDuration = snapshot.Average(x => x.TotalMilliseconds);

            Console.WriteLine("{0} {2} Completed, {1:F0}ms (average)", snapshot.Length, averageDuration, _description);
        }
    }
}