using Microsoft.Extensions.Logging.Abstractions;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista;

namespace Zenatur.LegacyBridge.UnitTests.OutboxHandlers;

public class MotoristaHandlerTests
{
    private static OutboxMessageDto Msg(string payload) =>
        new(1, "Motorista.Criado", payload, DateTime.UtcNow, 0);

    private static MotoristaCriadoHandler Handler() =>
        new(NullLogger<MotoristaCriadoHandler>.Instance, new MotoristaEnvelopeValidator());

    private const string ValidPayload = """
        {
          "schemaVersion": 1,
          "messageType": "Motorista.Criado",
          "occurredAt": "2026-05-14T15:20:00Z",
          "contratanteCnpj": "53717120000170",
          "motorista": {
            "cpf": "12447091826",
            "nome": "GILDASIO SANTANA DA CRUZ",
            "dataNascimento": "1980-07-22",
            "rntrc": "98765432",
            "telefoneDdd": "11",
            "telefoneNumero": "987654321",
            "endereco": { "logradouro": "RUA DOS CEDROS", "numero": 560, "bairro": "JARDIM SAPOPEMBA", "uf": "SP", "cep": "09973310" },
            "cnhNumero": "12345678901",
            "cnhCategoria": "E",
            "cnhValidade": "2030-06-30",
            "ativo": true
          }
        }
        """;

    [Fact]
    public void MessageType_Is_Motorista_Criado()
    {
        Assert.Equal("Motorista.Criado", Handler().MessageType);
        Assert.Equal(1, Handler().SupportedSchemaVersion);
    }

    [Fact]
    public async Task Valid_Payload_Returns_Ok()
    {
        var result = await Handler().HandleAsync(Msg(ValidPayload), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Malformed_Json_Returns_ValidationError()
    {
        var result = await Handler().HandleAsync(Msg("<<not json>>"), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
    }

    [Fact]
    public async Task Cpf_Wrong_Length_Returns_ValidationError()
    {
        var bad = ValidPayload.Replace("\"cpf\": \"12447091826\"", "\"cpf\": \"123\"");

        var result = await Handler().HandleAsync(Msg(bad), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
    }

    [Fact]
    public async Task Wrong_SchemaVersion_Returns_ValidationError()
    {
        var bad = ValidPayload.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2");

        var result = await Handler().HandleAsync(Msg(bad), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(result.Errors[0]);
    }
}
