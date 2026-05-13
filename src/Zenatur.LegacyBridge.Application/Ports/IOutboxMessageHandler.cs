using FluentResults;
using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.Application.Ports;

public interface IOutboxMessageHandler
{
    string MessageType { get; }

    int SupportedSchemaVersion { get; }

    Task<Result> HandleAsync(OutboxMessageDto message, CancellationToken ct);
}
