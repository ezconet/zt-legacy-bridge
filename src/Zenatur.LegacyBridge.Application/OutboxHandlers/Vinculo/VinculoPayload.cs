namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Vinculo;

public sealed record VinculoPayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    string FavorecidoDocumento,
    int FavorecidoDocumentoTipo,
    string MotoristaCpf);
