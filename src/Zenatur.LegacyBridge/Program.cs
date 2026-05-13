using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;
using Zenatur.LegacyBridge.Application.Common;
using Zenatur.LegacyBridge.Application.Queries.GetMotoristaByCpf;
using Zenatur.LegacyBridge.Endpoints;
using Zenatur.LegacyBridge.Infrastructure;
using Zenatur.LegacyBridge.Logging;
using Zenatur.LegacyBridge.Middleware;

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

    builder.Services.AddHealthChecks();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<GetMotoristaByCpfHandler>();

    var app = builder.Build();

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
    app.MapHealthChecks("/health/ready");

    app.MapMotoristasEndpoints();

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
