using FluentResults;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.Application.Ports;

public interface IMotoristaRepository
{
    Task<Result<MotoristaDto?>> GetByCpfAsync(string documento, CancellationToken ct);
}
