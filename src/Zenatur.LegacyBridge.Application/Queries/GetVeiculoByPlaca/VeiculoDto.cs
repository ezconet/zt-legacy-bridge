namespace Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

public sealed record VeiculoDto(
    string Placa,
    int? TipoVeiculo,
    string? Renavam,
    int? AnoFabricacao,
    int? AnoModelo,
    string? Marca,
    string? Modelo,
    decimal? Tara,
    decimal? CapacidadeKg,
    ProprietarioDto? Proprietario);

public sealed record ProprietarioDto(
    string Documento,
    string TipoDocumento,
    string Nome);
