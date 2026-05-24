// ReSharper disable VirtualMemberCallInConstructor

using AutoFixture;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.TestsUtilities;

public abstract class UnitTestsBase<TSubjectOfUnitTest> : IAsyncLifetime
    where TSubjectOfUnitTest : class
{
    protected readonly IFixture DataGenerator = new Fixture();
    protected TSubjectOfUnitTest Sut;
    protected IServiceProvider _sp;

    protected readonly ServiceCollection _services;
    
    protected UnitTestsBase(ITestOutputHelper output)
    {
        TestContext.Output = output;
        _services = new ServiceCollection();
        _services.AddTransient<TSubjectOfUnitTest>();
    }
    
    protected UnitTestsBase()
    {
        _services = new ServiceCollection();
        _services.AddTransient<TSubjectOfUnitTest>();
    }
    
    protected virtual void PrepareTestData(IFixture dataGenerator) { }
    protected virtual void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) { }
    
    public virtual async ValueTask InitializeAsync()
    {
        var autoSubstitute = new AutoSubstituteFixture();
        RegisterMockedDependencies(_services, autoSubstitute);
        
        // _services.AddLogging();
        
        _sp = _services.BuildServiceProvider();
        Sut = _sp.GetRequiredService<TSubjectOfUnitTest>();
        
        PrepareTestData(DataGenerator);
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