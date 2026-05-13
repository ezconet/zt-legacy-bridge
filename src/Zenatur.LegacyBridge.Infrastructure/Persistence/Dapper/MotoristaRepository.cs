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
                using var multi = await conn.QueryMultipleAsync(
                    new CommandDefinition(
                        Sql,
                        new { cpf },
                        commandTimeout: _factory.CommandTimeoutSeconds,
                        cancellationToken: token));

                var motoristaRow = await multi.ReadFirstOrDefaultAsync<MotoristaRow>();
                if (motoristaRow is null)
                {
                    return Result.Ok<MotoristaDto?>(null);
                }

                var veiculoRows = await multi.ReadAsync<VeiculoRow>();
                return Result.Ok<MotoristaDto?>(motoristaRow.ToDto(veiculoRows));
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
        public DateTime? AnttValidade { get; set; }

        public MotoristaDto ToDto(IEnumerable<VeiculoRow> veiculos) => new(
            Cpf: Cpf,
            Nome: Nome,
            DataNascimento: DataNascimento.HasValue ? DateOnly.FromDateTime(DataNascimento.Value) : null,
            Telefone: Telefone,
            Endereco: HasAnyAddressField()
                ? new EnderecoDto(Logradouro, Numero, Complemento, Bairro, CidadeIbge, Uf, Cep)
                : null,
            AnttValidade: AnttValidade.HasValue ? DateOnly.FromDateTime(AnttValidade.Value) : null,
            Veiculos: veiculos.Select(v => v.ToDto()).ToList());

        private bool HasAnyAddressField() =>
            !string.IsNullOrWhiteSpace(Logradouro) ||
            !string.IsNullOrWhiteSpace(Numero) ||
            !string.IsNullOrWhiteSpace(Complemento) ||
            !string.IsNullOrWhiteSpace(Bairro) ||
            !string.IsNullOrWhiteSpace(CidadeIbge) ||
            !string.IsNullOrWhiteSpace(Uf) ||
            !string.IsNullOrWhiteSpace(Cep);
    }

    private sealed class VeiculoRow
    {
        public string Placa { get; set; } = "";
        public int? TipoVeiculo { get; set; }
        public string? Renavam { get; set; }
        public int? Ano { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public double? Tara { get; set; }
        public double? CapacidadeKg { get; set; }
        public string? Rntrc { get; set; }

        public VeiculoMotoristaDto ToDto() => new(
            Placa: Placa,
            TipoVeiculo: TipoVeiculo,
            Renavam: Renavam,
            Ano: Ano,
            Marca: Marca,
            Modelo: Modelo,
            Tara: Tara is null ? null : (decimal)Tara.Value,
            CapacidadeKg: CapacidadeKg is null ? null : (decimal)CapacidadeKg.Value,
            Rntrc: Rntrc);
    }
}
