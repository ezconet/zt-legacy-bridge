using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido;

public sealed record FavorecidoEnvelopePayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    FavorecidoData Favorecido);

public sealed record FavorecidoData(
    string Documento,
    int DocumentoTipo,
    string Nome,
    DateOnly? DataNascimento,
    string? Email,
    string? Rntrc,
    string? RntrcSituacao,
    string? TelefoneDdd,
    string? TelefoneNumero,
    EnderecoPayload? Endereco,
    bool Ativo);

public sealed record ContaEventPayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    string FavorecidoDocumento,
    int FavorecidoDocumentoTipo,
    ContaData Conta);

public sealed record ContaData(
    long Id,
    int Banco,
    string Agencia,
    string? AgenciaDigito,
    string Numero,
    int Tipo,
    int? ChavePixTipo,
    string? ChavePix,
    string? PambankIndicador,
    string? Status);

public sealed record ContaRemovidaPayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    string FavorecidoDocumento,
    long ContaId);
