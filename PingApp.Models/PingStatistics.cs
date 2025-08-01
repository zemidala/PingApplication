using System.Collections.Generic;

namespace PingApp.Models
{
    public class PingStatistics
    {
        public long MinTime { get; set; } = long.MaxValue;
        public long MaxTime { get; set; } = 0;
        public double AverageTime { get; set; } = 0;
        public long CurrentTime { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;
        public int FailureCount { get; set; } = 0;
        public int TotalCount => SuccessCount + FailureCount;
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;

        public void UpdateWithResult(PingResult result)
        {
            if (result.IsSuccess)
            {
                SuccessCount++;
                CurrentTime = result.RoundTripTime;

                if (result.RoundTripTime < MinTime)
                    MinTime = result.RoundTripTime;

                if (result.RoundTripTime > MaxTime)
                    MaxTime = result.RoundTripTime;

                AverageTime = ((AverageTime * (SuccessCount - 1)) + result.RoundTripTime) / SuccessCount;
            }
            else
            {
                FailureCount++;
                CurrentTime = 0;
            }
        }
    }
}