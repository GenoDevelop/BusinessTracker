using GenoDev.BusinessTracker.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Infrastructure;

public class BusinessTrackerDbContextFactory : IDesignTimeDbContextFactory<BusinessTrackerDbContext>
{
    public BusinessTrackerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddInfrastructureSettings()
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructureServices(configuration);

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<BusinessTrackerDbContext>();
    }
}
