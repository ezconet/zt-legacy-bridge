using FluentResults;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;

namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed class GetMotoristaByCpfHandler
{
    private readonly IMotoristaRepository _repo;

    public GetMotoristaByCpfHandler(IMotoristaRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<MotoristaDto>> HandleAsync(GetMotoristaByCpfQuery query, CancellationToken ct)
    {
        var cpfDigits = SanitizeCpf(query.Cpf);
        if (cpfDigits.Length != 11)
        {
            return Result.Fail(new ValidationError(
                $"CPF must contain exactly 11 digits (got {cpfDigits.Length})."));
        }

        var lookup = await _repo.GetByCpfAsync(cpfDigits, ct);
        if (lookup.IsFailed)
        {
            return Result.Fail(lookup.Errors);
        }

        return lookup.Value is null
            ? Result.Fail(new NotFoundError("Motorista não encontrado"))
            : Result.Ok(lookup.Value);
    }

    private static string SanitizeCpf(string cpf) =>
        new(cpf.Where(char.IsDigit).ToArray());
}
