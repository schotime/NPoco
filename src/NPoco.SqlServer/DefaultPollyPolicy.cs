using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;

namespace NPoco.SqlServer
{
    public class DefaultPollyPolicy : IPollyPolicy
    {
        public virtual RetryPolicy RetryPolicy { get; set; } = RetryPolicyImp;

        public virtual AsyncRetryPolicy AsyncRetryPolicy { get; set; } = RetryPolicyAsyncImp;

        public static IEnumerable<TimeSpan> RetryTimes { get; set; } = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(5)
        };

        private static readonly AsyncRetryPolicy RetryPolicyAsyncImp = Policy
            .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
            .Or<TimeoutException>()
            .WaitAndRetryAsync(RetryTimes);

        private static readonly RetryPolicy RetryPolicyImp = Policy
            .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
            .Or<TimeoutException>()
            .WaitAndRetry(RetryTimes);
    }
}