using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;
using Zenatur.LegacyBridge.Middleware;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "logs/bridge-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWindowsService(o => o.ServiceName = "ZenaturLegacyBridge");

    builder.Host.UseSerilog((ctx, _, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseMiddleware<ApiKeyMiddleware>());

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready");

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
