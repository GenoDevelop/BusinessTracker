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
        
        services.AddDbContext<BusinessTrackerDbContext>(builder => builder.UseNpgsql(connectionString, opt =>
        {
            BusinessTrackerDbContext.ModifyOptionsBuilder(opt);
        }));
        services.AddScoped<IBusinessTrackerDbContext, BusinessTrackerDbContext>();
        
        return services;
    }
}