using System.Net;
using Zenatur.LegacyBridge.IntegrationTests.Fixtures;

namespace Zenatur.LegacyBridge.IntegrationTests.Endpoints;

[Collection("Legacy DB")]
public class VeiculosEndpointsTests
{
    private readonly LegacyDbFixture _fixture;

    public VeiculosEndpointsTests(LegacyDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Returns_200_With_Veiculo_Payload_On_Mercosul_Match()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/veiculos/ABC1D23");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"placa\":\"ABC1D23\"", body);
        Assert.Contains("\"marca\":\"VOLVO\"", body);
        Assert.Contains("\"proprietario\"", body);
        Assert.Contains("\"tipoDocumento\":\"CPF\"", body);
        Assert.Contains("\"documento\":\"12345678901\"", body);
    }

    [Fact]
    public async Task Returns_200_When_Stored_Placa_Has_Hyphen()
    {
        // Real legacy data uses 'XYZ-9876' (with hyphen). Caller normalises to 'XYZ9876'.
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/veiculos/XYZ9876");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"marca\":\"SCANIA\"", body);
        Assert.Contains("\"tipoDocumento\":\"CNPJ\"", body);
        Assert.Contains("\"documento\":\"12345678000199\"", body);
    }

    [Fact]
    public async Task Returns_404_When_Placa_Not_Found()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/veiculos/AAA9Z99");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_400_When_Placa_Has_Wrong_Length()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/veiculos/ABC");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_400_When_Placa_Format_Unrecognised()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/veiculos/1234567");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_401_Without_ApiKey_Header()
    {
        var resp = await _fixture.AnonymousClient().GetAsync("/v1/legacy/veiculos/ABC1D23");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
