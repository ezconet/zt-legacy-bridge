using FluentResults;
using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.Application.Ports;

public interface IOutboxDispatcher
{
    Task<Result> DispatchAsync(OutboxMessageDto message, CancellationToken ct);
}
