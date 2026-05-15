namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao;

public sealed record ComposicaoEnvelopePayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    ComposicaoData Composicao);

public sealed record ComposicaoData(
    long Id,
    string Nome,
    string TracionantePlaca,
    IReadOnlyList<ReboquePayload> Reboques,
    string? FavorecidoTitularDocumento,
    bool Ativo);

public sealed record ReboquePayload(
    string Placa,
    int Ordem);

public sealed record ComposicaoRemovidaPayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    long ComposicaoId);
