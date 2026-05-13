using System.Security.Cryptography;
using System.Text;

namespace Zenatur.LegacyBridge.Middleware;

public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";
    private const string ConfigKey = "InboundApiKey";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly byte[] _expectedBytes;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var expected = config[ConfigKey];
        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new InvalidOperationException(
                $"Required configuration '{ConfigKey}' is missing or empty.");
        }
        _expectedBytes = Encoding.UTF8.GetBytes(expected);
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var provided)
            || string.IsNullOrWhiteSpace(provided))
        {
            _logger.LogWarning(
                "ApiKeyMissing {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);
            await Reject(ctx);
            return;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        if (providedBytes.Length != _expectedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(providedBytes, _expectedBytes))
        {
            _logger.LogWarning(
                "ApiKeyMissing {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);
            await Reject(ctx);
            return;
        }

        await _next(ctx);
    }

    private static Task Reject(HttpContext ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return ctx.Response.WriteAsync("Unauthorized");
    }
}
