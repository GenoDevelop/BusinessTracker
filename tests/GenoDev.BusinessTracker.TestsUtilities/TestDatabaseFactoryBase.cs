using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.TestsUtilities;

/// <summary>
/// Serves as an abstract base class for creating a test database factory.
/// Facilitates the registration, initialization, and resetting of a test database
/// to support applications or services that require database interaction during testing.
/// </summary>
public abstract class TestDatabaseFactoryBase : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Registers the database configuration and dependencies into the provided service collection.
    /// This method is intended to be overridden in a derived class to customize database registration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> where the database services will be registered.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> with the database services added.</returns>
    public abstract IServiceCollection RegisterDatabase(IServiceCollection services);

    /// <summary>
    /// Resets the test database to a clean state, ensuring it is ready for the next test execution.
    /// This method is typically used to clear data, reapply migrations, or reset any database-specific configurations.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> instance used to resolve the required services for database operations.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation of resetting the database.</returns>
    public abstract Task ResetDatabaseAsync(IServiceProvider sp);

    /// <summary>
    /// Initializes the test database and performs any necessary setup required
    /// before using it during testing. This method allows customization of the
    /// initialization behavior in derived classes.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> instance used to resolve the required services for database initialization.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation of initializing the test database.</returns>
    public abstract Task InitializeAsync(IServiceProvider sp);

    /// <inheritdoc/>
    public abstract ValueTask DisposeAsync();

    /// <inheritdoc/>
    public abstract void Dispose();
}