namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo;

public sealed record VeiculoEnvelopePayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    VeiculoData Veiculo);

public sealed record VeiculoData(
    string Placa,
    string? Renavam,
    string? CategoriaVeiculo,
    string? TipoTracao,
    int? Eixos,
    int? AnoFabricacao,
    int? AnoModelo,
    string? Marca,
    string? Modelo,
    string? Cor,
    decimal? Tara,
    decimal? CapacidadeKg,
    string? Rntrc,
    string? RntrcSituacao,
    DateOnly? RntrcValidade,
    string? ProprietarioFavorecidoDocumento,
    bool Ativo);

public sealed record VeiculoRemovidoPayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    string Placa);
