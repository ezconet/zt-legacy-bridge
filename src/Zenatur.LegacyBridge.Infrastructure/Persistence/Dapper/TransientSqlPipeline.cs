using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;

namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

internal static class TransientSqlPipeline
{
    private static readonly int[] TransientErrorNumbers =
    {
        1205,   // deadlock victim
        4060,   // cannot open database
        40197,  // service error processing
        40501,  // service busy
        40613,  // database unavailable
        49918, 49919, 49920, // throttling
    };

    public static readonly ResiliencePipeline Instance = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<SqlException>(
                ex => Array.IndexOf(TransientErrorNumbers, ex.Number) >= 0),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = false,
        })
        .Build();
}
