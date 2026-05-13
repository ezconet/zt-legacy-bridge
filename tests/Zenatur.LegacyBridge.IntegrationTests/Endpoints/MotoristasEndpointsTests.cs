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
    public async Task Returns_200_With_Motorista_Payload_On_Match()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/12345678901");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"cpf\":\"12345678901\"", body);
        Assert.Contains("\"nome\":\"JOAO DA SILVA\"", body);
        Assert.Contains("\"rntrc\"", body);
        Assert.Contains("\"endereco\"", body);
    }

    [Fact]
    public async Task Returns_404_When_Cpf_Not_Found()
    {
        var resp = await _fixture.AuthorizedClient().GetAsync("/v1/legacy/motoristas/99999999999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_400_When_Cpf_Has_Wrong_Length()
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
