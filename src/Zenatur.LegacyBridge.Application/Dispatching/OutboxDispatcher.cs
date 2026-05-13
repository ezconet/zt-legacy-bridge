using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.Dispatching;

public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly IReadOnlyDictionary<string, IOutboxMessageHandler> _handlers;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IEnumerable<IOutboxMessageHandler> handlers, ILogger<OutboxDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(h => h.MessageType, h => h, StringComparer.Ordinal);
        _logger = logger;
    }

    public Task<Result> DispatchAsync(OutboxMessageDto message, CancellationToken ct)
    {
        if (!_handlers.TryGetValue(message.MessageType, out var handler))
        {
            _logger.LogError(
                "UnknownMessageType {MessageType} OutboxId {OutboxId}",
                message.MessageType, message.Id);
            return Task.FromResult(Result.Fail(new ValidationError(
                $"No handler registered for MessageType '{message.MessageType}'")));
        }

        int version;
        try
        {
            using var doc = JsonDocument.Parse(message.Payload);
            if (!doc.RootElement.TryGetProperty("schemaVersion", out var versionEl)
                || versionEl.ValueKind != JsonValueKind.Number)
            {
                _logger.LogError(
                    "SchemaVersionMissing OutboxId {OutboxId} Type {MessageType}",
                    message.Id, message.MessageType);
                return Task.FromResult(Result.Fail(new ValidationError(
                    "Payload missing required integer 'schemaVersion'")));
            }
            version = versionEl.GetInt32();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "PayloadParseError OutboxId {OutboxId} Type {MessageType}",
                message.Id, message.MessageType);
            return Task.FromResult(Result.Fail(new ValidationError(
                $"Payload is not valid JSON: {ex.Message}")));
        }

        if (version != handler.SupportedSchemaVersion)
        {
            _logger.LogError(
                "SchemaVersionMismatch handler={Handler} expected={Expected} got={Got} OutboxId {OutboxId}",
                handler.GetType().Name, handler.SupportedSchemaVersion, version, message.Id);
            return Task.FromResult(Result.Fail(new ValidationError(
                $"Handler {handler.GetType().Name} supports schema v{handler.SupportedSchemaVersion}, got v{version}")));
        }

        return handler.HandleAsync(message, ct);
    }
}
