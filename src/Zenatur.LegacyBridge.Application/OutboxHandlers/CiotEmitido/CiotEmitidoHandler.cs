using FluentResults;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.CiotEmitido;

public sealed class CiotEmitidoHandler : IOutboxMessageHandler
{
    public const string Type = "Ciot.Emitido";
    public const int SchemaVersion = 1;

    private readonly ILogger<CiotEmitidoHandler> _logger;

    public CiotEmitidoHandler(ILogger<CiotEmitidoHandler> logger)
    {
        _logger = logger;
    }

    public string MessageType => Type;
    public int SupportedSchemaVersion => SchemaVersion;

    public Task<Result> HandleAsync(OutboxMessageDto message, CancellationToken ct)
    {
        // Stub — L34 replaces with real validation + dedupe + writes.
        _logger.LogInformation(
            "CiotEmitido stub no-op OutboxId {OutboxId}", message.Id);
        return Task.FromResult(Result.Ok());
    }
}
