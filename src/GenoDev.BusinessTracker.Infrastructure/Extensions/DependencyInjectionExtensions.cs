using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BusinessTracker");
        
        services.AddDbContextFactory<BusinessTrackerDbContext>(builder => builder.UseNpgsql(connectionString, opt =>
        {
            BusinessTrackerDbContext.ModifyOptionsBuilder(opt);
        }));
        services.AddTransient<IBusinessTrackerDbContext>(s => s.GetRequiredService<IDbContextFactory<BusinessTrackerDbContext>>().CreateDbContext());
        
        return services;
    }
}