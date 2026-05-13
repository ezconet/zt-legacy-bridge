using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Zenatur.LegacyBridge.HealthChecks;

public sealed class CiotApiHealthCheck : IHealthCheck
{
    public const string HttpClientName = "ciot-health";

    private readonly IHttpClientFactory _factory;

    public CiotApiHealthCheck(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient(HttpClientName);
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, "/");
            using var resp = await client.SendAsync(req, cancellationToken);
            // Any HTTP response (even 4xx) means the API is up; only network failure → Unhealthy.
            return HealthCheckResult.Healthy($"CIOT API reachable: HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CIOT API unreachable", ex);
        }
    }
}
