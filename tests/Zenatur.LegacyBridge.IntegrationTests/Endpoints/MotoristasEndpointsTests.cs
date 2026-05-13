using System.Net;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace Zenatur.LegacyBridge.IntegrationTests.Endpoints;

public class MotoristasEndpointsTests : IAsyncLifetime
{
    private const string ApiKey = "test-key-xyz";

    private MsSqlContainer _sql = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _sql.StartAsync();
        await SeedAsync(_sql.GetConnectionString());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InboundApiKey"] = ApiKey,
                    ["LegacyDb:ConnectionString"] = _sql.GetConnectionString(),
                    ["LegacyDb:CommandTimeoutSeconds"] = "10",
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _sql.DisposeAsync();
    }

    private static async Task SeedAsync(string connStr)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            CREATE TABLE dbo.MOTORISTAS (
                NR_CPF        VARCHAR(11)   NOT NULL PRIMARY KEY,
                NM_MOT        NVARCHAR(200) NOT NULL,
                DT_NASC       DATE          NULL,
                NR_TELEFONE   VARCHAR(20)   NULL,
                DS_LOGRADOURO NVARCHAR(200) NULL,
                NR_ENDERECO   VARCHAR(20)   NULL,
                DS_COMPL      NVARCHAR(100) NULL,
                DS_BAIRRO     NVARCHAR(100) NULL,
                CD_IBGE       VARCHAR(7)    NULL,
                SG_UF         CHAR(2)       NULL,
                NR_CEP        VARCHAR(8)    NULL,
                NR_RNTRC      VARCHAR(8)    NULL,
                FL_RNTRC_ATIVO BIT          NULL,
                DT_RNTRC_VAL  DATE          NULL
            );");

        await conn.ExecuteAsync(@"
            INSERT INTO dbo.MOTORISTAS
                (NR_CPF, NM_MOT, DT_NASC, NR_TELEFONE,
                 DS_LOGRADOURO, NR_ENDERECO, DS_BAIRRO,
                 CD_IBGE, SG_UF, NR_CEP,
                 NR_RNTRC, FL_RNTRC_ATIVO, DT_RNTRC_VAL)
            VALUES
                ('12345678901', 'JOAO DA SILVA', '1980-03-15', '11999998888',
                 'RUA X', '100', 'CENTRO',
                 '3550308', 'SP', '01310100',
                 '12345678', 1, '2027-01-31');");
    }

    private HttpClient AuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    [Fact]
    public async Task Returns_200_With_Motorista_Payload_On_Match()
    {
        var resp = await AuthorizedClient().GetAsync("/v1/legacy/motoristas/12345678901");

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
        var resp = await AuthorizedClient().GetAsync("/v1/legacy/motoristas/99999999999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_400_When_Cpf_Has_Wrong_Length()
    {
        var resp = await AuthorizedClient().GetAsync("/v1/legacy/motoristas/123");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Returns_401_Without_ApiKey_Header()
    {
        var resp = await _factory.CreateClient().GetAsync("/v1/legacy/motoristas/12345678901");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
