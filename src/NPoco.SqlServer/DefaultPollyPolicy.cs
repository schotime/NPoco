using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using NPoco.SqlServer;
using Polly;
using Polly.Retry;

namespace NPoco.DatabaseTypes
{
    public class DefaultPollyPolicy : IPollyPolicy
    {
        public virtual RetryPolicy RetryPolicy { get; set; } = RetryPolicyImp;

        public virtual AsyncRetryPolicy AsyncRetryPolicy { get; set; } = RetryPolicyAsyncImp;

        public static IEnumerable<TimeSpan> RetryTimes { get; set; } = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3)
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