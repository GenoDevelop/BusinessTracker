using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Behaviors;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using FluentValidation;
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
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddApplicationValidators();

        // WPF resolves MediatR requests from the host provider without per-request scopes.
        // Keep this service aligned with its transient DbContext dependency.
        services.AddTransient<IItemsService, ItemsService>();
        services.AddTransient<IMailTemplateRenderer, MailTemplateRenderer>();

        return services;
    }

    private static void AddApplicationValidators(this IServiceCollection services)
    {
        var validatorInterfaceType = typeof(IValidator<>);
        var applicationAssembly = typeof(DependencyInjectionExtensions).Assembly;

        var registrations = applicationAssembly.DefinedTypes
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.ImplementedInterfaces
                .Where(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == validatorInterfaceType)
                .Select(@interface => new
                {
                    ServiceType = @interface,
                    ImplementationType = type.AsType()
                }));

        foreach (var registration in registrations)
        {
            services.AddTransient(registration.ServiceType, registration.ImplementationType);
        }
    }
}
