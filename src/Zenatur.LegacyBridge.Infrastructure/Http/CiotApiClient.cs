using System.Net.Http.Json;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Infrastructure.Http;

public sealed class CiotApiClient : ICiotApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static readonly ResiliencePipeline<HttpResponseMessage> DefaultPipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
            })
            .Build();

    private readonly HttpClient _http;
    private readonly ILogger<CiotApiClient> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public CiotApiClient(HttpClient http, ILogger<CiotApiClient> logger)
        : this(http, logger, DefaultPipeline)
    {
    }

    public CiotApiClient(
        HttpClient http,
        ILogger<CiotApiClient> logger,
        ResiliencePipeline<HttpResponseMessage> pipeline)
    {
        _http = http;
        _logger = logger;
        _pipeline = pipeline;
    }

    public async Task<Result<IReadOnlyList<OutboxMessageDto>>> GetPendingAsync(int size, CancellationToken ct)
    {
        try
        {
            var resp = await _pipeline.ExecuteAsync(
                async token => await _http.GetAsync($"/api/v1/outbox/pending?size={size}", token),
                ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "CiotApiUnreachable GET /outbox/pending → {StatusCode}", resp.StatusCode);
                return Result.Fail<IReadOnlyList<OutboxMessageDto>>(
                    new Application.Common.InfraError($"CIOT API GetPending returned {(int)resp.StatusCode}"));
            }

            var list = await resp.Content.ReadFromJsonAsync<List<OutboxMessageDto>>(JsonOpts, ct)
                       ?? new List<OutboxMessageDto>();
            return Result.Ok<IReadOnlyList<OutboxMessageDto>>(list);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "CiotApiUnreachable circuit open");
            return Result.Fail<IReadOnlyList<OutboxMessageDto>>(
                new Application.Common.InfraError("CIOT API circuit open").CausedBy(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CiotApiUnreachable on GetPending");
            return Result.Fail<IReadOnlyList<OutboxMessageDto>>(
                new Application.Common.InfraError("CiotApiUnreachable").CausedBy(ex));
        }
    }

    public async Task<Result> AckAsync(AckRequest request, CancellationToken ct)
    {
        try
        {
            var resp = await _pipeline.ExecuteAsync(async token =>
            {
                using var content = JsonContent.Create(request, options: JsonOpts);
                return await _http.PostAsync("/api/v1/outbox/ack", content, token);
            }, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "AckFailed POST /outbox/ack OutboxId {OutboxId} → {StatusCode}",
                    request.Id, resp.StatusCode);
                return Result.Fail(new Application.Common.InfraError(
                    $"CIOT API Ack returned {(int)resp.StatusCode} for OutboxId {request.Id}"));
            }

            return Result.Ok();
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "AckFailed circuit open OutboxId {OutboxId}", request.Id);
            return Result.Fail(new Application.Common.InfraError("CIOT API circuit open").CausedBy(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AckFailed OutboxId {OutboxId}", request.Id);
            return Result.Fail(new Application.Common.InfraError("AckFailed").CausedBy(ex));
        }
    }
}
