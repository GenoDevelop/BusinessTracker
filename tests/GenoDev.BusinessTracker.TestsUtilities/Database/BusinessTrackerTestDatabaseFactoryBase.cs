using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.TestsUtilities.Database;

public abstract class BusinessTrackerTestDatabaseFactoryBase : TestDatabaseFactoryBase
{
    public abstract override IServiceCollection RegisterDatabase(IServiceCollection services);
    public abstract override Task ResetDatabaseAsync(IServiceProvider sp);

    public abstract override Task InitializeAsync(IServiceProvider sp);
    public abstract override ValueTask DisposeAsync();

    public override void Dispose()
    {
    }
}