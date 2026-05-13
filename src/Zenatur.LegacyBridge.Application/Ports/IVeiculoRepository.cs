using FluentResults;
using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.Application.Ports;

public interface IVeiculoRepository
{
    Task<Result<VeiculoDto?>> GetByPlacaAsync(string placa, CancellationToken ct);
}
