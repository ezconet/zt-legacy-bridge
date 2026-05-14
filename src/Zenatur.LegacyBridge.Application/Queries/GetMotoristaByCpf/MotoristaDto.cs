using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;

namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed record MotoristaDto(
    string Documento,
    string TipoDocumento,
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
    string? Uf,
    string? Cep);
