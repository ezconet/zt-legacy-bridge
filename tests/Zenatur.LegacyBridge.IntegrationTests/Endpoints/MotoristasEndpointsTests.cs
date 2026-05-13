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
            CREATE TABLE dbo.fornecedor (
                cod_forn       INT             NOT NULL PRIMARY KEY,
                nome_forn      NVARCHAR(1000)  NOT NULL,
                endereco_forn  NVARCHAR(100)   NULL,
                bairro_forn    NVARCHAR(100)   NULL,
                cidade_forn    NVARCHAR(100)   NULL,
                uf_forn        NVARCHAR(4)     NULL,
                cgc_forn       NVARCHAR(40)    NULL,
                cep_forn       NVARCHAR(20)    NULL,
                tel1_forn      NVARCHAR(40)    NULL,
                dt_antt        SMALLDATETIME   NULL
            );
            CREATE TABLE dbo.TB_DocumentoMotorista (
                cod_forn  INT          NOT NULL PRIMARY KEY,
                cpf       VARCHAR(11)  NULL
            );
            CREATE TABLE dbo.TB_DadosVeiculo (
                cod_forn  INT           NOT NULL PRIMARY KEY,
                placa     VARCHAR(30)   NOT NULL,
                rntrc     VARCHAR(8)    NULL,
                idTipo    INT           NULL,
                ano       INT           NULL,
                marca     VARCHAR(30)   NULL,
                modelo    VARCHAR(30)   NULL,
                tara      FLOAT         NULL,
                pesoReal  FLOAT         NULL,
                renavam   VARCHAR(11)   NULL
            );");

        await conn.ExecuteAsync(@"
            INSERT INTO dbo.fornecedor
                (cod_forn, nome_forn, endereco_forn, bairro_forn, uf_forn,
                 cgc_forn, cep_forn, tel1_forn, dt_antt)
            VALUES
                (1, 'JOAO DA SILVA', 'RUA X, 100', 'CENTRO', 'SP',
                 '12345678901', '01310100', '11999998888', '2027-01-31');

            INSERT INTO dbo.TB_DocumentoMotorista (cod_forn, cpf)
            VALUES (1, '12345678901');

            INSERT INTO dbo.TB_DadosVeiculo
                (cod_forn, placa, rntrc, idTipo, ano, marca, modelo, tara, pesoReal, renavam)
            VALUES
                (1, 'ABC1D23', '12345678', 1, 2018, 'VOLVO', 'FH 540', 8500, 25000, '00123456789');");
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
