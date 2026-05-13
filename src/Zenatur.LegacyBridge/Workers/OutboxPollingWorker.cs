using FluentResults;
using Microsoft.Extensions.Options;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Infrastructure.Http;

namespace Zenatur.LegacyBridge.Workers;

public sealed class OutboxPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly CiotApiOptions _opts;
    private readonly ILogger<OutboxPollingWorker> _logger;

    public OutboxPollingWorker(
        IServiceScopeFactory scopes,
        IOptions<CiotApiOptions> opts,
        ILogger<OutboxPollingWorker> logger)
    {
        _scopes = scopes;
        _opts = opts.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPollingWorker started batch={BatchSize} idleMs={Idle} busyMs={Busy} backoffMs={Backoff}",
            _opts.BatchSize, _opts.PollIntervalIdleMs, _opts.PollIntervalBusyMs, _opts.BackoffMsOnFailure);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IterateAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPollingWorker iteration crashed");
                await DelayAsync(_opts.BackoffMsOnFailure, stoppingToken);
            }
        }

        _logger.LogInformation("OutboxPollingWorker stopped");
    }

    private async Task IterateAsync(CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ICiotApiClient>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

        var fetch = await client.GetPendingAsync(_opts.BatchSize, ct);
        if (fetch.IsFailed)
        {
            _logger.LogWarning("OutboxFetched fail: {Errors}", Stringify(fetch.Errors));
            await DelayAsync(_opts.BackoffMsOnFailure, ct);
            return;
        }

        var msgs = fetch.Value;
        if (msgs.Count == 0)
        {
            await DelayAsync(_opts.PollIntervalIdleMs, ct);
            return;
        }

        _logger.LogInformation("OutboxFetched count={Count}", msgs.Count);

        foreach (var msg in msgs)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessAsync(client, dispatcher, msg, ct);
        }

        await DelayAsync(_opts.PollIntervalBusyMs, ct);
    }

    private async Task ProcessAsync(
        ICiotApiClient client,
        IOutboxDispatcher dispatcher,
        OutboxMessageDto msg,
        CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(msg, ct);

        var ack = new AckRequest(
            msg.Id,
            result.IsSuccess,
            result.IsSuccess ? null : Stringify(result.Errors));

        var ackResult = await client.AckAsync(ack, ct);

        if (ackResult.IsFailed)
        {
            _logger.LogError(
                "AckFailed OutboxId {OutboxId} type {MessageType}: {Errors}",
                msg.Id, msg.MessageType, Stringify(ackResult.Errors));
            return;
        }

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "MessageProcessed OutboxId {OutboxId} type {MessageType} retry {RetryCount}",
                msg.Id, msg.MessageType, msg.RetryCount);
        }
        else
        {
            _logger.LogWarning(
                "MessageHandlerFailed OutboxId {OutboxId} type {MessageType} retry {RetryCount}: {Errors}",
                msg.Id, msg.MessageType, msg.RetryCount, Stringify(result.Errors));
        }
    }

    private static async Task DelayAsync(int milliseconds, CancellationToken ct)
    {
        if (milliseconds <= 0) return;
        try
        {
            await Task.Delay(milliseconds, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string Stringify(IEnumerable<IError> errors) =>
        string.Join("; ", errors.Select(e => e.Message));
}
