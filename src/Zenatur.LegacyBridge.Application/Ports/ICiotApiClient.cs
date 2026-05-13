using FluentResults;
using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.Application.Ports;

public interface ICiotApiClient
{
    Task<Result<IReadOnlyList<OutboxMessageDto>>> GetPendingAsync(int size, CancellationToken ct);

    Task<Result> AckAsync(AckRequest request, CancellationToken ct);
}
