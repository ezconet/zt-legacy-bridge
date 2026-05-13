using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
