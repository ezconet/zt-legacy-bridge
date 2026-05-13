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
        var documento = SanitizeDigits(query.Documento);
        if (documento.Length != 11 && documento.Length != 14)
        {
            return Result.Fail(new ValidationError(
                $"Documento must contain 11 (CPF) or 14 (CNPJ) digits (got {documento.Length})."));
        }

        var maskedDoc = documento.Length == 14 ? PiiMask.Cnpj(documento) : PiiMask.Cpf(documento);

        var lookup = await _repo.GetByCpfAsync(documento, ct);
        if (lookup.IsFailed)
        {
            return Result.Fail(lookup.Errors);
        }

        if (lookup.Value is null)
        {
            _logger.LogWarning("QueryNotFound favorecido documento={DocumentoMasked}", maskedDoc);
            return Result.Fail(new NotFoundError("Favorecido não encontrado"));
        }

        _logger.LogInformation("QuerySucceeded favorecido documento={DocumentoMasked}", maskedDoc);
        return Result.Ok(lookup.Value);
    }

    private static string SanitizeDigits(string input) =>
        new(input.Where(char.IsDigit).ToArray());
}
