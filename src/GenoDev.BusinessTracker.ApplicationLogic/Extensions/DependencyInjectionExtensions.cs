using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Behaviors;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // WPF resolves MediatR requests from the host provider without per-request scopes.
        // Keep this service aligned with its transient DbContext dependency.
        services.AddTransient<IItemsService, ItemsService>();

        return services;
    }
}
