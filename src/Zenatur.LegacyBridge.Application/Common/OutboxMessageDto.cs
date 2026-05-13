namespace Zenatur.LegacyBridge.Application.Common;

public sealed record OutboxMessageDto(
    long Id,
    string MessageType,
    string Payload,
    DateTime CreatedAt,
    int RetryCount);
