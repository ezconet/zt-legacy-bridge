using Dapper;
using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

public sealed class MotoristaRepository : IMotoristaRepository
{
    private static readonly string Sql = SqlResources.Load("motorista_by_cpf.sql");

    private readonly DapperConnectionFactory _factory;
    private readonly ILogger<MotoristaRepository> _logger;

    public MotoristaRepository(DapperConnectionFactory factory, ILogger<MotoristaRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<Result<MotoristaDto?>> GetByCpfAsync(string cpf, CancellationToken ct)
    {
        try
        {
            return await TransientSqlPipeline.Instance.ExecuteAsync(async token =>
            {
                await using var conn = await _factory.CreateOpenAsync(token);
                var row = await conn.QuerySingleOrDefaultAsync<MotoristaRow>(
                    new CommandDefinition(
                        Sql,
                        new { cpf },
                        commandTimeout: _factory.CommandTimeoutSeconds,
                        cancellationToken: token));
                return Result.Ok<MotoristaDto?>(row?.ToDto());
            }, ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "LegacyDbUnreachable on motorista lookup");
            return Result.Fail(new InfraError("LegacyDbUnreachable").CausedBy(ex));
        }
    }

    private sealed class MotoristaRow
    {
        public string Cpf { get; set; } = "";
        public string Nome { get; set; } = "";
        public DateTime? DataNascimento { get; set; }
        public string? Telefone { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? CidadeIbge { get; set; }
        public string? Uf { get; set; }
        public string? Cep { get; set; }
        public string? RntrcNumero { get; set; }
        public bool? RntrcAtivo { get; set; }
        public DateTime? RntrcValidade { get; set; }

        public MotoristaDto ToDto() => new(
            Cpf: Cpf,
            Nome: Nome,
            DataNascimento: DataNascimento.HasValue ? DateOnly.FromDateTime(DataNascimento.Value) : null,
            Telefone: Telefone,
            Endereco: HasAnyAddressField()
                ? new EnderecoDto(Logradouro, Numero, Complemento, Bairro, CidadeIbge, Uf, Cep)
                : null,
            Rntrc: !string.IsNullOrWhiteSpace(RntrcNumero)
                ? new RntrcDto(
                    RntrcNumero,
                    RntrcAtivo,
                    RntrcValidade.HasValue ? DateOnly.FromDateTime(RntrcValidade.Value) : null)
                : null);

        private bool HasAnyAddressField() =>
            !string.IsNullOrWhiteSpace(Logradouro) ||
            !string.IsNullOrWhiteSpace(Numero) ||
            !string.IsNullOrWhiteSpace(Complemento) ||
            !string.IsNullOrWhiteSpace(Bairro) ||
            !string.IsNullOrWhiteSpace(CidadeIbge) ||
            !string.IsNullOrWhiteSpace(Uf) ||
            !string.IsNullOrWhiteSpace(Cep);
    }
}
