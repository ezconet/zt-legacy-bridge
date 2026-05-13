using FluentResults;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

public sealed class GetVeiculoByPlacaHandler
{
    private readonly IVeiculoRepository _repo;
    private readonly ILogger<GetVeiculoByPlacaHandler> _logger;

    public GetVeiculoByPlacaHandler(
        IVeiculoRepository repo,
        ILogger<GetVeiculoByPlacaHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Result<VeiculoDto>> HandleAsync(GetVeiculoByPlacaQuery query, CancellationToken ct)
    {
        var placa = SanitizePlaca(query.Placa);
        if (placa.Length != 7)
        {
            return Result.Fail(new ValidationError(
                $"Placa must contain exactly 7 alphanumeric chars (got {placa.Length})."));
        }

        if (!IsKnownFormat(placa))
        {
            return Result.Fail(new ValidationError(
                $"Placa format not recognised. Expected Mercosul (AAA0A00) or antigo (AAA0000), got '{placa}'."));
        }

        var lookup = await _repo.GetByPlacaAsync(placa, ct);
        if (lookup.IsFailed)
        {
            return Result.Fail(lookup.Errors);
        }

        if (lookup.Value is null)
        {
            _logger.LogWarning("QueryNotFound veiculo placa={Placa}", placa);
            return Result.Fail(new NotFoundError("Veículo não encontrado"));
        }

        _logger.LogInformation("QuerySucceeded veiculo placa={Placa}", placa);
        return Result.Ok(lookup.Value);
    }

    private static string SanitizePlaca(string placa) =>
        new string(placa.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static bool IsKnownFormat(string p)
    {
        if (p.Length != 7) return false;
        if (!(char.IsLetter(p[0]) && char.IsLetter(p[1]) && char.IsLetter(p[2]))) return false;
        if (!char.IsDigit(p[3])) return false;
        var mercosul = char.IsLetter(p[4]) && char.IsDigit(p[5]) && char.IsDigit(p[6]);
        var antigo = char.IsDigit(p[4]) && char.IsDigit(p[5]) && char.IsDigit(p[6]);
        return mercosul || antigo;
    }
}
