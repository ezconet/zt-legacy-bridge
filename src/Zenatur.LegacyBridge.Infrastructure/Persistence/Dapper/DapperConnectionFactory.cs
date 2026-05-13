using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

public sealed class DapperConnectionFactory
{
    private readonly LegacyDbOptions _opts;

    public DapperConnectionFactory(IOptions<LegacyDbOptions> opts)
    {
        _opts = opts.Value;
        if (string.IsNullOrWhiteSpace(_opts.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Required configuration '{LegacyDbOptions.SectionName}:ConnectionString' is missing or empty.");
        }
    }

    public int CommandTimeoutSeconds => _opts.CommandTimeoutSeconds;

    public async Task<SqlConnection> CreateOpenAsync(CancellationToken ct)
    {
        var conn = new SqlConnection(_opts.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
