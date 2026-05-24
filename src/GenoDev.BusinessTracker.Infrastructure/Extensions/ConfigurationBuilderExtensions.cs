using Microsoft.Extensions.Configuration;

namespace GenoDev.BusinessTracker.Infrastructure.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddInfrastructureSettings(this IConfigurationBuilder builder)
    {
        builder.AddJsonFile("infrastructure_settings.json", optional: true, reloadOnChange: true);
        return builder;
    }
}
