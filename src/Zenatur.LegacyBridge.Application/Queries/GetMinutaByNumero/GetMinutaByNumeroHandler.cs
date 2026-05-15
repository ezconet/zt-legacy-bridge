using FluentResults;
using Microsoft.Extensions.Logging;
using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.Application.Queries.GetMinutaByNumero;

public sealed class GetMinutaByNumeroHandler
{
    private readonly ILogger<GetMinutaByNumeroHandler> _logger;

    public GetMinutaByNumeroHandler(ILogger<GetMinutaByNumeroHandler> logger)
    {
        _logger = logger;
    }

    public Task<Result<MinutaDto>> HandleAsync(GetMinutaByNumeroQuery query, CancellationToken ct)
    {
        var numero = (query.NumeroMinuta ?? "").Trim();
        if (numero.Length == 0)
        {
            return Task.FromResult(
                Result.Fail<MinutaDto>(new ValidationError("numeroMinuta is required.")));
        }

        // Sentinel for the 404 contract until the real legacy lookup lands.
        if (numero is "0" or "000000")
        {
            _logger.LogWarning("QueryNotFound minuta numero={Numero}", numero);
            return Task.FromResult(
                Result.Fail<MinutaDto>(new NotFoundError("Minuta não encontrada")));
        }

        _logger.LogInformation("QuerySucceeded minuta numero={Numero} (mock)", numero);

        var full = BuildMock(numero);
        var dto = numero switch
        {
            // Favorecido/motorista incompletos: sem dataNascimento + endereço parcial.
            "654321" => full with
            {
                Favorecido = full.Favorecido with
                {
                    DataNascimento = null,
                    Endereco = new MinutaEnderecoDto(
                        "Rua das Transportadoras", null, null, "Centro",
                        "Sao Paulo", "SP", null, null),
                },
                Motorista = full.Motorista with
                {
                    DataNascimento = null,
                    CnhNumero = null,
                    CnhCategoria = null,
                    CnhValidade = null,
                    RgNumero = null,
                    RgUf = null,
                },
            },
            // Sem veículo (legado não tem).
            "456789" => full with { Veiculo = null },
            // Sem pontos de parada.
            "987654" => full with { PontosParada = null },
            _ => full,
        };

        return Task.FromResult(Result.Ok(dto));
    }

    private static MinutaDto BuildMock(string numero) => new(
        Meta: new MinutaMetaDto(
            Endpoint: "GET /v1/legacy/minutas/{numeroMinuta}",
            BaseUrl: "Zenatur LegacyBridge (mesma base de /v1/legacy/motoristas e /v1/legacy/veiculos)",
            Auth: "X-Api-Key (BridgeApiKeyHandler — igual aos outros endpoints legacy)",
            ConsumidoPor: "Zenatur.Tms.Infrastructure.LegacyBridge.HttpLegacyBridgeClient.GetMinutaAsync",
            Notas: new[]
            {
                "404 quando a minuta nao existe no legado.",
                "Campos null = legado nao tem o dado; TMS marca o bloco como 'completar'.",
                "documentoTipo: 1=CNPJ, 2=CPF (convencao Pamcard ja usada no projeto).",
                "ibge: codigo IBGE 7 digitos da cidade (usado p/ rota/pedagio Pamcard).",
                "valorFreteLegado: valor que o legado pagaria ao favorecido (comparar c/ frete minimo ANTT).",
                "JSON camelCase (HttpLegacyBridgeClient usa JsonNamingPolicy.CamelCase, case-insensitive).",
                "MOCK — implementacao legado pendente B6.",
            }),
        NumeroMinuta: numero,
        Favorecido: new MinutaFavorecidoDto(
            Documento: "13294646720",
            DocumentoTipo: 2,
            Nome: "JOAO MOTORISTA TAC",
            DataNascimento: new DateOnly(1985, 3, 12),
            Rntrc: "12345678",
            TelefoneDdd: "011",
            TelefoneNumero: "987654321",
            Endereco: new MinutaEnderecoDto(
                "Rua das Transportadoras", "100", "Galpao 3", "Centro",
                "Sao Paulo", "SP", "01001000", 3550308)),
        Motorista: new MinutaMotoristaDto(
            Cpf: "13294646720",
            Nome: "JOAO MOTORISTA TAC",
            DataNascimento: new DateOnly(1985, 3, 12),
            CnhNumero: "01234567890",
            CnhCategoria: "E",
            CnhValidade: new DateOnly(2028, 3, 12),
            RgNumero: "123456789",
            RgUf: "SP",
            TelefoneDdd: "011",
            TelefoneNumero: "987654321"),
        Veiculo: new MinutaVeiculoDto(
            Placa: "ABC1D23",
            TipoVeiculoLegado: new MinutaTipoVeiculoDto(7, "Cavalo Mecanico Trucado"),
            Renavam: "00123456789",
            AnoFabricacao: 2019,
            AnoModelo: 2020,
            Marca: "VOLVO",
            Modelo: "FH 540",
            Rntrc: "87654321",
            Reboques: new[]
            {
                new MinutaReboqueDto(1, "XYZ4567", new MinutaTipoVeiculoDto(12, "Semi-Reboque")),
            }),
        Documentos: new[]
        {
            new MinutaDocumentoDto("NFe", 6, "1001", "1",
                "35200612345678000190550010000010011000010019", 25000.00m),
            new MinutaDocumentoDto("CTe", 5, "5001", "1",
                "35200612345678000190570010000050011000050017", 25000.00m),
        },
        Origem: new MinutaLocalDto("Sao Paulo", "SP", 3550308),
        Destino: new MinutaLocalDto("Rio de Janeiro", "RJ", 3304557),
        PontosParada: new[]
        {
            new MinutaPontoParadaDto(1, "Guarulhos", "SP", 3518800,
                "Rodovia Presidente Dutra, km 225", "S/N", "Coleta"),
            new MinutaPontoParadaDto(2, "Resende", "RJ", 3304300,
                "Av. das Industrias", "4500", "Entrega Parcial"),
        },
        ValorFreteLegado: 4500.00m);
}
