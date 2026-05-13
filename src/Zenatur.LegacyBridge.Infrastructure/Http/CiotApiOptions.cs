namespace Zenatur.LegacyBridge.Infrastructure.Http;

public sealed class CiotApiOptions
{
    public const string SectionName = "CiotApi";

    public string BaseUrl { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public int BatchSize { get; init; } = 50;
    public int PollIntervalIdleMs { get; init; } = 5000;
    public int PollIntervalBusyMs { get; init; } = 500;
    public int BackoffMsOnFailure { get; init; } = 10000;
    public int HttpTimeoutSeconds { get; init; } = 30;
}
