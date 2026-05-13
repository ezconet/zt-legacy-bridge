using Dapper;
using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

public sealed class VeiculoRepository : IVeiculoRepository
{
    private static readonly string Sql = SqlResources.Load("veiculo_by_placa.sql");

    private readonly DapperConnectionFactory _factory;
    private readonly ILogger<VeiculoRepository> _logger;

    public VeiculoRepository(DapperConnectionFactory factory, ILogger<VeiculoRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<Result<VeiculoDto?>> GetByPlacaAsync(string placa, CancellationToken ct)
    {
        try
        {
            return await TransientSqlPipeline.Instance.ExecuteAsync(async token =>
            {
                await using var conn = await _factory.CreateOpenAsync(token);
                var row = await conn.QuerySingleOrDefaultAsync<VeiculoRow>(
                    new CommandDefinition(
                        Sql,
                        new { placa },
                        commandTimeout: _factory.CommandTimeoutSeconds,
                        cancellationToken: token));
                return Result.Ok<VeiculoDto?>(row?.ToDto());
            }, ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "LegacyDbUnreachable on veiculo lookup");
            return Result.Fail(new InfraError("LegacyDbUnreachable").CausedBy(ex));
        }
    }

    private sealed class VeiculoRow
    {
        public string Placa { get; set; } = "";
        public int? TipoVeiculo { get; set; }
        public string? Renavam { get; set; }
        public int? AnoFabricacao { get; set; }
        public int? AnoModelo { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public double? Tara { get; set; }
        public double? CapacidadeKg { get; set; }
        public string? PropDocumento { get; set; }
        public string? PropNome { get; set; }
        public string? PropTipoDocumento { get; set; }

        public VeiculoDto ToDto() => new(
            Placa: Placa,
            TipoVeiculo: TipoVeiculo,
            Renavam: Renavam,
            AnoFabricacao: AnoFabricacao,
            AnoModelo: AnoModelo,
            Marca: Marca,
            Modelo: Modelo,
            Tara: Tara is null ? null : (decimal)Tara.Value,
            CapacidadeKg: CapacidadeKg is null ? null : (decimal)CapacidadeKg.Value,
            Proprietario: !string.IsNullOrWhiteSpace(PropDocumento) && !string.IsNullOrWhiteSpace(PropNome)
                ? new ProprietarioDto(PropDocumento, PropTipoDocumento ?? "CPF", PropNome)
                : null);
    }
}
