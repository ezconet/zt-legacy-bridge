using System.Text.Json.Serialization;

namespace Zenatur.LegacyBridge.Application.Queries.GetMinutaByNumero;

public sealed record MinutaDto(
    [property: JsonPropertyName("_meta")] MinutaMetaDto Meta,
    string NumeroMinuta,
    MinutaFavorecidoDto Favorecido,
    MinutaMotoristaDto Motorista,
    MinutaVeiculoDto? Veiculo,
    IReadOnlyList<MinutaDocumentoDto> Documentos,
    MinutaLocalDto Origem,
    MinutaLocalDto Destino,
    IReadOnlyList<MinutaPontoParadaDto>? PontosParada,
    decimal? ValorFreteLegado);

public sealed record MinutaMetaDto(
    string Endpoint,
    string BaseUrl,
    string Auth,
    string ConsumidoPor,
    IReadOnlyList<string> Notas);

public sealed record MinutaEnderecoDto(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep,
    int? Ibge);

public sealed record MinutaFavorecidoDto(
    string Documento,
    int DocumentoTipo,
    string? Nome,
    DateOnly? DataNascimento,
    string? Rntrc,
    string? TelefoneDdd,
    string? TelefoneNumero,
    MinutaEnderecoDto? Endereco);

public sealed record MinutaMotoristaDto(
    string Cpf,
    string? Nome,
    DateOnly? DataNascimento,
    string? CnhNumero,
    string? CnhCategoria,
    DateOnly? CnhValidade,
    string? RgNumero,
    string? RgUf,
    string? TelefoneDdd,
    string? TelefoneNumero);

public sealed record MinutaTipoVeiculoDto(
    int Id,
    string? Descricao);

public sealed record MinutaReboqueDto(
    int Ordem,
    string Placa,
    MinutaTipoVeiculoDto? TipoVeiculoLegado);

public sealed record MinutaVeiculoDto(
    string Placa,
    MinutaTipoVeiculoDto? TipoVeiculoLegado,
    string? Renavam,
    int? AnoFabricacao,
    int? AnoModelo,
    string? Marca,
    string? Modelo,
    string? Rntrc,
    IReadOnlyList<MinutaReboqueDto> Reboques);

public sealed record MinutaDocumentoDto(
    string Tipo,
    int TipoCodigo,
    string Numero,
    string? Serie,
    string? Chave,
    decimal? Valor);

public sealed record MinutaLocalDto(
    string? Cidade,
    string? Uf,
    int? Ibge);

public sealed record MinutaPontoParadaDto(
    int Ordem,
    string? Cidade,
    string? Uf,
    int? Ibge,
    string? Logradouro,
    string? Numero,
    string? Tipo);
