using Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista;

public sealed record MotoristaEnvelopePayload(
    int SchemaVersion,
    string MessageType,
    DateTimeOffset OccurredAt,
    string ContratanteCnpj,
    MotoristaData Motorista);

public sealed record MotoristaData(
    string Cpf,
    string Nome,
    DateOnly? DataNascimento,
    string? Email,
    string? Rntrc,
    string? RntrcSituacao,
    DateOnly? RntrcValidade,
    string? TelefoneDdd,
    string? TelefoneNumero,
    string? CelularDdd,
    string? CelularNumero,
    EnderecoPayload? Endereco,
    string? CnhNumero,
    string? CnhCategoria,
    DateOnly? CnhValidade,
    bool Ativo);
