using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace Zenatur.LegacyBridge.IntegrationTests.Fixtures;

public sealed class LegacyDbFixture : IAsyncLifetime
{
    public const string ApiKey = "test-key-shared";

    public MsSqlContainer Sql { get; private set; } = null!;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await Sql.StartAsync();
        await SeedAsync(Sql.GetConnectionString());

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InboundApiKey"] = ApiKey,
                    ["LegacyDb:ConnectionString"] = Sql.GetConnectionString(),
                    ["LegacyDb:CommandTimeoutSeconds"] = "10",
                    ["CiotApi:BaseUrl"] = "http://localhost:65535",
                    ["CiotApi:ApiKey"] = "unused-in-tests",
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Sql.DisposeAsync();
    }

    public HttpClient AuthorizedClient()
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return c;
    }

    public HttpClient AnonymousClient() => Factory.CreateClient();

    private static async Task SeedAsync(string connStr)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        await conn.ExecuteAsync("CREATE DATABASE DB_WMS;");
        await conn.ExecuteAsync(@"
            CREATE TABLE [DB_WMS].dbo.TB_REC_Veiculos (
                idVeiculo INT          NOT NULL PRIMARY KEY,
                Descricao VARCHAR(50)  NULL
            );
            INSERT INTO [DB_WMS].dbo.TB_REC_Veiculos (idVeiculo, Descricao)
            VALUES (1, 'CAMINHAO 3/4'), (2, 'TRUCK / TOCO');");

        // bd_fin_zenatur cross-DB JOIN dropped — dataNascimento intentionally null,
        // cidadeIbge removed from DTO. endereco_forn now carries trailing-digit numero
        // via SQL parser (kept logradouro intact).

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
                (1, 'JOAO DA SILVA', 'RUA X 100', 'CENTRO', 'SP',
                 '12345678901', '01310100', '11999998888', '2027-01-31'),
                (2, 'TRANSPORTES X LTDA', 'AV CENTRAL 500', 'INDUSTRIAL', 'SP',
                 '12345678000199', '04500000', '1133334444', '2028-06-30');

            INSERT INTO dbo.TB_DocumentoMotorista (cod_forn, cpf)
            VALUES (1, '12345678901');

            INSERT INTO dbo.TB_DadosVeiculo
                (cod_forn, placa, rntrc, idTipo, ano, marca, modelo, tara, pesoReal, renavam)
            VALUES
                (1, 'ABC1D23', '12345678', 1, 2018, 'VOLVO', 'FH 540', 8500, 25000, '00123456789'),
                (2, 'XYZ-9876', '87654321', 2, 2020, 'SCANIA', 'R 450', 9000, 30000, '00987654321');");

        // Note: TB_DadosVeiculo PK is cod_forn (1:1 with fornecedor) per current legacy schema.
        // Multi-vehicle assertions in tests rely on the fact that motorista lookup returns
        // the 0-or-1 vehicle bound to the matched cod_forn.
    }
}

[CollectionDefinition("Legacy DB")]
public sealed class LegacyDbCollection : ICollectionFixture<LegacyDbFixture>
{
}
