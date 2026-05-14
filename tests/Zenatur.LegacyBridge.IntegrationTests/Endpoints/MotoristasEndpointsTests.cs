using System.Net;
using Zenatur.LegacyBridge.IntegrationTests.Fixtures;

namespace Zenatur.LegacyBridge.IntegrationTests.Endpoints;

[Collection("Legacy DB")]
public class MotoristasEndpointsTests
{
    private readonly LegacyDbFixture _fixture;

    public MotoristasEndpointsTests(LegacyDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Returns_200_With_Motorista_Payload_On_Cpf_Match()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/12345678901");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"documento\":\"12345678901\"", body);
        Assert.Contains("\"tipoDocumento\":\"CPF\"", body);
        Assert.Contains("\"nome\":\"JOAO DA SILVA\"", body);
        Assert.Contains("\"endereco\"", body);
        // From bd_fin_zenatur.tb_motorista LEFT JOIN
        Assert.Contains("\"dataNascimento\":\"1980-03-15\"", body);
        Assert.Contains("\"numero\":\"1500\"", body);
        Assert.Contains("\"complemento\":\"APTO 304\"", body);
        // From bd_fin_zenatur.tb_cidade JOIN
        Assert.Contains("\"cidadeIbge\":\"3550308\"", body);
        Assert.Contains("\"anttValidade\"", body);
        Assert.Contains("\"veiculos\"", body);
        Assert.Contains("\"placa\":\"ABC1D23\"", body);
        Assert.Contains("\"marca\":\"VOLVO\"", body);
        Assert.Contains("\"descricao\":\"CAMINHAO 3/4\"", body);
        Assert.DoesNotContain("\"proprietario\":{", body);
    }

    [Fact]
    public async Task Returns_200_With_Pj_Payload_On_Cnpj_Match()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/12345678000199");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"documento\":\"12345678000199\"", body);
        Assert.Contains("\"tipoDocumento\":\"CNPJ\"", body);
        Assert.Contains("\"nome\":\"TRANSPORTES X LTDA\"", body);
    }

    [Fact]
    public async Task Returns_404_When_Documento_Not_Found()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/99999999999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_400_When_Documento_Has_Wrong_Length()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/123");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_401_Without_ApiKey_Header()
    {
        var resp = await _fixture.AnonymousClient().GetAsync("/v1/legacy/motoristas/12345678901");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
