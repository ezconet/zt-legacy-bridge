namespace Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;

public sealed record MotoristaDto(
    string Cpf,
    string Nome,
    DateOnly? DataNascimento,
    string? Telefone,
    EnderecoDto? Endereco,
    DateOnly? AnttValidade,
    IReadOnlyList<VeiculoMotoristaDto> Veiculos);

public sealed record EnderecoDto(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CidadeIbge,
    string? Uf,
    string? Cep);

public sealed record VeiculoMotoristaDto(
    string Placa,
    int? TipoVeiculo,
    string? Renavam,
    int? Ano,
    string? Marca,
    string? Modelo,
    decimal? Tara,
    decimal? CapacidadeKg,
    string? Rntrc);
