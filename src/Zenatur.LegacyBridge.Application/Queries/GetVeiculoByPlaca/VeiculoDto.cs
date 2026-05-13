namespace Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

public sealed record VeiculoDto(
    string Placa,
    TipoVeiculoDto? TipoVeiculo,
    string? Renavam,
    int? AnoFabricacao,
    int? AnoModelo,
    string? Marca,
    string? Modelo,
    decimal? Tara,
    decimal? CapacidadeKg,
    string? Rntrc,
    ProprietarioDto? Proprietario);

public sealed record TipoVeiculoDto(
    int Id,
    string? Descricao);

public sealed record ProprietarioDto(
    string Documento,
    string TipoDocumento,
    string Nome);
