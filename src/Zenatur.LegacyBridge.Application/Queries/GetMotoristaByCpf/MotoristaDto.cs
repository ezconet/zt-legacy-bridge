using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed record MotoristaDto(
    string Cpf,
    string Nome,
    DateOnly? DataNascimento,
    string? Telefone,
    EnderecoDto? Endereco,
    DateOnly? AnttValidade,
    IReadOnlyList<VeiculoDto> Veiculos);

public sealed record EnderecoDto(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CidadeIbge,
    string? Uf,
    string? Cep);
