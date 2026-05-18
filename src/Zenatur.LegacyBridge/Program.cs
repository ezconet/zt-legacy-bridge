using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Formatting.Compact;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Dispatching;
using Zenatur.LegacyBridge.Application.OutboxHandlers.CiotEmitido;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Application.Queries.GetMinutaByNumero;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;
using Zenatur.LegacyBridge.Application.Queries.GetVeiculoByPlaca;
using Zenatur.LegacyBridge.Endpoints;
using Zenatur.LegacyBridge.HealthChecks;
using Zenatur.LegacyBridge.Infrastructure;
using Zenatur.LegacyBridge.Infrastructure.Http;
using Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;
using Zenatur.LegacyBridge.Logging;
using Zenatur.LegacyBridge.Middleware;
using Zenatur.LegacyBridge.Workers;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "logs/bridge-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    builder.Host.UseWindowsService(o => o.ServiceName = "ZenaturLegacyBridge");

    builder.Host.UseSerilog((ctx, _, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.With<RequestPathMaskingEnricher>());

    builder.Services.ConfigureHttpJsonOptions(o =>
    {
        o.SerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpClient(CiotApiHealthCheck.HttpClientName, (sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<CiotApiOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            client.BaseAddress = new Uri(opts.BaseUrl);
        }
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    builder.Services.AddSingleton<CiotApiHealthCheck>();
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionStringFactory: sp => sp.GetRequiredService<IOptions<LegacyDbOptions>>().Value.ConnectionString,
            healthQuery: "SELECT 1;",
            name: "legacy-db",
            tags: new[] { "ready" })
        .AddCheck<CiotApiHealthCheck>(
            name: "ciot-api",
            tags: new[] { "ready" });
    builder.Services.AddScoped<GetMotoristaByCpfHandler>();
    builder.Services.AddScoped<GetVeiculoByPlacaHandler>();
    builder.Services.AddScoped<GetMinutaByNumeroHandler>();

    builder.Services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
    builder.Services.AddScoped<IOutboxMessageHandler, CiotEmitidoHandler>();

    builder.Services.AddValidatorsFromAssemblyContaining<
        Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoEnvelopeValidator>();

    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoCriadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoAtualizadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoContaAdicionadaHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoContaAtualizadaHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Favorecido.FavorecidoContaRemovidaHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista.MotoristaCriadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Motorista.MotoristaAtualizadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Vinculo.FavorecidoMotoristaVinculadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Vinculo.FavorecidoMotoristaDesvinculadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo.VeiculoCriadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo.VeiculoAtualizadoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Veiculo.VeiculoRemovidoHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao.ComposicaoCriadaHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao.ComposicaoAtualizadaHandler>();
    builder.Services.AddScoped<IOutboxMessageHandler, Zenatur.LegacyBridge.Application.OutboxHandlers.Composicao.ComposicaoRemovidaHandler>();

    builder.Services.AddHostedService<OutboxPollingWorker>();

    var app = builder.Build();

    // Publicado sob sub-caminho no IIS/proxy (ex.: /bridge). Sem isto o
    // roteamento responde só na raiz e dá 404 atrás do prefixo.
    var pathBase = builder.Configuration["PathBase"];
    if (!string.IsNullOrWhiteSpace(pathBase))
        app.UsePathBase(pathBase);

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPathMasked} responded {StatusCode} in {Elapsed:0.0000} ms";
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestPathMasked", PiiMask.Path(ctx.Request.Path));
        };
    });

    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseMiddleware<ApiKeyMiddleware>());

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    app.MapMotoristasEndpoints();
    app.MapVeiculosEndpoints();
    app.MapMinutasEndpoints();

    if (app.Environment.IsEnvironment("Testing"))
    {
        app.MapGet("/__test/secured-ping", () => Results.Ok("pong"));
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bridge host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
