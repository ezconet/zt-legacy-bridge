using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zenatur.LegacyBridge.Application.Ports;
using Zenatur.LegacyBridge.Infrastructure.Http;
using Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

namespace Zenatur.LegacyBridge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptions<LegacyDbOptions>()
            .Bind(config.GetSection(LegacyDbOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<DapperConnectionFactory>();
        services.AddScoped<IMotoristaRepository, MotoristaRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();

        services
            .AddOptions<CiotApiOptions>()
            .Bind(config.GetSection(CiotApiOptions.SectionName))
            .ValidateOnStart();

        services.AddTransient<ApiKeyDelegatingHandler>();
        services.AddHttpClient<ICiotApiClient, CiotApiClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CiotApiOptions>>().Value;
                if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                {
                    throw new InvalidOperationException(
                        $"Required configuration '{CiotApiOptions.SectionName}:BaseUrl' is missing or empty.");
                }
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
            })
            .AddHttpMessageHandler<ApiKeyDelegatingHandler>();

        return services;
    }
}
