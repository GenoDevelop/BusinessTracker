using System.Diagnostics;
using System.Runtime.InteropServices;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GenoDev.BusinessTracker.TestsUtilities.Database;

public class BusinessTrackerPostgreSqlContainer
{
    private const string ContainerName = "business_tracker_test_db_reusable";
    
    static BusinessTrackerPostgreSqlContainer()
    {
        InitializeAsync().GetAwaiter().GetResult();
    }

    private static PostgreSqlContainer? _container;

    private static PostgreSqlContainer BuildContainer()
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithName(ContainerName)
            .WithUsername("genodev")
            .WithPassword("totally_strong_password")
            .WithDatabase("business_tracker_db")
            .WithCommand("-c", "fsync=off",
                "-c", "full_page_writes=off",
                "-c", "synchronous_commit=off",
                "-c", "max_prepared_transactions=10000")
            .WithReuse(true)
            .WithLabel("reuse-id", "business_tracker-test-db")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(PostgreSqlBuilder.PostgreSqlPort))
            .Build();
    }

    private static string? _connectionString;
    public static string ConnectionString => _connectionString ??= _container!.GetConnectionString() + ";Include Error Detail=true";

    private static NpgsqlConnection? _connection;
    public static NpgsqlConnection Connection => _connection ??= DataSource.CreateConnection();

    private static NpgsqlDataSource? _dataSource;
    public static NpgsqlDataSource DataSource
    {
        get
        {
            if (_dataSource is not null)
                return _dataSource;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
            _dataSource = dataSourceBuilder.Build();
            return _dataSource;
        }
    }

    public static async Task ReloadDataSource()
    {
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        
        _dataSource = null;
    }

    public static async Task<bool> IsMigrationApplied<TDbContext>(TDbContext context) where TDbContext : DbContext
    {
        var latestMigration = context.GetService<IMigrationsAssembly>().Migrations.Keys.LastOrDefault();
        if (latestMigration is null)
            return true;

        var result = await _container!.ExecAsync(["cat", $"/tmp/{GetMigrationLabelKey<TDbContext>()}"]);
        return result.ExitCode == 0 && result.Stdout.Trim() == latestMigration;
    }

    private static async Task InitializeAsync()
    {
        _container = BuildContainer();
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("Conflict") && ex.Message.Contains(ContainerName))
        {
            // If there's a name conflict, the existing container might be dangling or have different settings.
            // We force remove it and try one more time.
            await KillContainerAsync();
            _container = BuildContainer();
            await _container.StartAsync();
        }
    }

    public static async Task ReinitializeAsync()
    {
        await KillContainerAsync();
        await InitializeAsync();
    }

    public static async Task KillContainerAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync(); 

        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        
        if (_connection is not null)
            await _connection.DisposeAsync();

        _container = null;
        _connectionString = null;
        _connection = null;
        _dataSource = null;
        
        // Even if _container was disposed, there might be a dangling container with the same name in Docker
        // that Testcontainers is not tracking in the current session.
        try
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var fileName = isWindows ? "cmd.exe" : "docker";
            var arguments = isWindows ? $"/c docker rm -f {ContainerName}" : $"rm -f {ContainerName}";

            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var process = Process.Start(processInfo);
            if (process != null)
                await process.WaitForExitAsync();
        }
        catch
        {
            // Ignore errors if docker command fails
        }
    }

    public static async Task MarkMigrationAsApplied<TDbContext>(TDbContext context) where TDbContext : DbContext
    {
        var latestMigration = context.GetService<IMigrationsAssembly>().Migrations.Keys.LastOrDefault();
        if (latestMigration is null)
            return;

        await _container!.ExecAsync(["sh", "-c", $"echo {latestMigration} > /tmp/{GetMigrationLabelKey<TDbContext>()}"]);
    }

    private static string GetMigrationLabelKey<TDbContext>() => $"latest-migration-{typeof(TDbContext).Name}";
}