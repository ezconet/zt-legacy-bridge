namespace Zenatur.LegacyBridge.Application.OutboxHandlers.Common;

public sealed record EnderecoPayload(
    string? Logradouro,
    int? Numero,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep,
    int? CidadeIbge);
