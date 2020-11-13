using Polly.Retry;

namespace NPoco.SqlServer
{
    public interface IPollyPolicy
    {
        RetryPolicy RetryPolicy { get; set; }
        AsyncRetryPolicy AsyncRetryPolicy { get; set; }
    }
}