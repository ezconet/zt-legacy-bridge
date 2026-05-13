using Microsoft.Extensions.Options;

namespace Zenatur.LegacyBridge.Infrastructure.Http;

public sealed class ApiKeyDelegatingHandler : DelegatingHandler
{
    public const string HeaderName = "X-Api-Key";

    private readonly string _apiKey;

    public ApiKeyDelegatingHandler(IOptions<CiotApiOptions> opts)
    {
        var key = opts.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"Required configuration '{CiotApiOptions.SectionName}:ApiKey' is missing or empty.");
        }
        _apiKey = key;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
