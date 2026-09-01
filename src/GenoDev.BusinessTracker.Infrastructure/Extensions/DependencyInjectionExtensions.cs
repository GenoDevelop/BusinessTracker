using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GenoDev.BusinessTracker.Infrastructure.Services;

namespace GenoDev.BusinessTracker.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BusinessTracker");
        
        services.AddDbContextFactory<BusinessTrackerDbContext>(builder => builder.UseNpgsql(connectionString, BusinessTrackerDbContext.ModifyOptionsBuilder));
        services.AddTransient<IBusinessTrackerDbContext>(s => s.GetRequiredService<IDbContextFactory<BusinessTrackerDbContext>>().CreateDbContext());
        services.AddSingleton<IMailOutboxProcessor, MailOutboxProcessor>();
        
        return services;
    }
}
