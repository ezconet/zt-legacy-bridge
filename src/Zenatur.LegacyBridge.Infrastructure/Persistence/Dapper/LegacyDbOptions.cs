namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

public sealed class LegacyDbOptions
{
    public const string SectionName = "LegacyDb";

    public string ConnectionString { get; init; } = "";
    public int CommandTimeoutSeconds { get; init; } = 30;
}
