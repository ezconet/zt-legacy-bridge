using FluentResults;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed class GetMotoristaByCpfHandler
{
    private readonly IMotoristaRepository _repo;
    private readonly ILogger<GetMotoristaByCpfHandler> _logger;

    public GetMotoristaByCpfHandler(
        IMotoristaRepository repo,
        ILogger<GetMotoristaByCpfHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Result<MotoristaDto>> HandleAsync(GetMotoristaByCpfQuery query, CancellationToken ct)
    {
        var cpfDigits = SanitizeCpf(query.Cpf);
        if (cpfDigits.Length != 11)
        {
            return Result.Fail(new ValidationError(
                $"CPF must contain exactly 11 digits (got {cpfDigits.Length})."));
        }

        var maskedCpf = PiiMask.Cpf(cpfDigits);

        var lookup = await _repo.GetByCpfAsync(cpfDigits, ct);
        if (lookup.IsFailed)
        {
            return Result.Fail(lookup.Errors);
        }

        if (lookup.Value is null)
        {
            _logger.LogWarning("QueryNotFound motorista cpf={CpfMasked}", maskedCpf);
            return Result.Fail(new NotFoundError("Motorista não encontrado"));
        }

        _logger.LogInformation("QuerySucceeded motorista cpf={CpfMasked}", maskedCpf);
        return Result.Ok(lookup.Value);
    }

    private static string SanitizeCpf(string cpf) =>
        new(cpf.Where(char.IsDigit).ToArray());
}
