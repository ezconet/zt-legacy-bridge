namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed record MotoristaDto(
    string Cpf,
    string Nome,
    DateOnly? DataNascimento,
    string? Telefone,
    EnderecoDto? Endereco,
    RntrcDto? Rntrc);

public sealed record EnderecoDto(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CidadeIbge,
    string? Uf,
    string? Cep);

public sealed record RntrcDto(
    string? Numero,
    bool? Ativo,
    DateOnly? Validade);
