// ReSharper disable VirtualMemberCallInConstructor

using AutoFixture;
using GenoDev.BusinessTracker.Infrastructure;
using GenoDev.BusinessTracker.TestsUtilities.Database;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.TestsUtilities;

public abstract class BusinessTrackerUnitTestsBase<TSubjectOfUnitTest> : IAsyncLifetime
    where TSubjectOfUnitTest : class
{
    private readonly List<BusinessTrackerTestPostgresDatabaseFactory> _databaseFactories = [];
    protected readonly List<BusinessTrackerTestPostgresDatabaseFactory> _disposingOrder = [];
    private IServiceScope? _arrangeScope;
    private IServiceScope? _assertScope;

    private IServiceScope GetArrangeScope() => _arrangeScope ??= _sp.CreateScope();
    private IServiceScope GetAssertScope() => _assertScope ??= _sp.CreateScope();
    
    protected readonly IFixture DataGenerator = new Fixture();
    protected TSubjectOfUnitTest Sut;
    protected IServiceProvider _sp;

    protected readonly ServiceCollection _services;
    
    protected BusinessTrackerUnitTestsBase(ITestOutputHelper output)
    {
        TestContext.Output = output;
        _services = new ServiceCollection();
        _services.AddTransient<TSubjectOfUnitTest>();
    }
    
    protected BusinessTrackerUnitTestsBase()
    {
        _services = new ServiceCollection();
        _services.AddTransient<TSubjectOfUnitTest>();
    }

    protected IServiceCollection RegisterBusinessTrackingPostgresDatabase(IServiceCollection services)
    {
        RegisterDatabase(services, BusinessTrackerTestPostgresDatabaseFactory.Instance);

        return services;
    }
    
    protected virtual void PrepareTestData(IFixture dataGenerator) { }
    protected virtual void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) { }
    
    public virtual async ValueTask InitializeAsync()
    {
        RegisterMockedDependencies(_services, new AutoSubstituteFixture());
        _sp = _services.BuildServiceProvider();

        var initializationTasks = _databaseFactories.Select(databaseFactory => databaseFactory.InitializeAsync(_sp));
        await Task.WhenAll(initializationTasks);

        Sut = _sp.GetRequiredService<TSubjectOfUnitTest>();
        PrepareTestData(DataGenerator);
    }

    protected T ArrangeBusinessTracker_Database<T>(Func<BusinessTrackerDbContext, T> arrange,
        Func<IServiceProvider, BusinessTrackerDbContext>? customContextInjection = null)
    {
        var scope = GetArrangeScope();
        var scopedService = scope.ServiceProvider;
        var db = customContextInjection?.Invoke(scopedService) ??
                 scopedService.GetRequiredService<BusinessTrackerDbContext>();

        var result = arrange.Invoke(db);
        db.SaveChanges();

        return result;
    }
    
    protected void ArrangeBusinessTracker_Database(Action<BusinessTrackerDbContext> arrange,
        Func<IServiceProvider, BusinessTrackerDbContext>? customContextInjection = null)
    {
        var scope = GetArrangeScope();
        var scopedService = scope.ServiceProvider;
        var db = customContextInjection?.Invoke(scopedService) ??
                 scopedService.GetRequiredService<BusinessTrackerDbContext>();

        arrange.Invoke(db);
        db.SaveChanges();
    }

    protected void AssertBusinessTracker_Database(Action<BusinessTrackerDbContext> assert,
        Func<IServiceProvider, BusinessTrackerDbContext>? customContextInjection = null)
    {
        var scope = GetAssertScope();
        var scopedService = scope.ServiceProvider;
        var db = customContextInjection?.Invoke(scopedService) ??
                 scopedService.GetRequiredService<BusinessTrackerDbContext>();

        assert.Invoke(db);
    }

    protected T AssertBusinessTracker_Database<T>(Func<BusinessTrackerDbContext, T> assert,
        Func<IServiceProvider, BusinessTrackerDbContext>? customContextInjection = null)
    {
        var scope = GetAssertScope();
        var scopedService = scope.ServiceProvider;
        var db = customContextInjection?.Invoke(scopedService) ??
                 scopedService.GetRequiredService<BusinessTrackerDbContext>();

        return assert.Invoke(db);
    }

    protected virtual IServiceCollection RegisterDatabase(IServiceCollection services,
        BusinessTrackerTestPostgresDatabaseFactory instance)
    {
        if (_databaseFactories.Any(x => x.GetType() == instance.GetType()))
            throw new Exception("Database factory of this type is already registered");

        instance.RegisterDatabase(services);

        _databaseFactories.Add(instance);
        _disposingOrder.Add(instance);

        return services;
    }
    
    public virtual async ValueTask DisposeAsync()
    {
        _disposingOrder.AddRange(_databaseFactories);
        foreach (var databaseFactory in _disposingOrder.Distinct())
            await databaseFactory.ResetDatabaseAsync(_sp);

        _disposingOrder.Clear();
        _databaseFactories.Clear();

        _arrangeScope?.Dispose();
        _arrangeScope = null;

        _assertScope?.Dispose();
        _assertScope = null;
    }
}

[Collection("UnitTestsCollection")]
public abstract class UnitTestsBase : IAsyncLifetime
{
    protected readonly IFixture DataGenerator = new Fixture();
    protected IServiceProvider _sp;

    protected readonly ServiceCollection _services;

    protected UnitTestsBase(ITestOutputHelper output)
    {
        TestContext.Output = output;
        _services = new ServiceCollection();
    }

    protected UnitTestsBase()
    {
        _services = new ServiceCollection();
    }

    protected virtual void PrepareTestData(IFixture dataGenerator)
    {
    }

    protected virtual void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
    }

    public virtual ValueTask InitializeAsync()
    {
        RegisterMockedDependencies(_services, new AutoSubstituteFixture());

        _sp = _services.BuildServiceProvider();

        PrepareTestData(DataGenerator);

        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DisposeAsync()
    {
        if (_sp is IDisposable disposable)
        {
            disposable.Dispose();
        }
        TestContext.Output = null;
        return ValueTask.CompletedTask;
    }
}