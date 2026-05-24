using System.Data.Common;
using System.Transactions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace GenoDev.BusinessTracker.TestsUtilities.Database;

// ReSharper disable once ClassNeverInstantiated.Global
public class BusinessTrackerTestPostgresDatabaseFactory : BusinessTrackerTestDatabaseFactoryBase
{
    private bool _initialized;
    
    private static BusinessTrackerTestPostgresDatabaseFactory? _instance;
    public static BusinessTrackerTestPostgresDatabaseFactory Instance => _instance ??= new BusinessTrackerTestPostgresDatabaseFactory();
    
    private static NpgsqlDataSource _dataSource => BusinessTrackerPostgreSqlContainer.DataSource;
    private static DbConnection _dbConnection => BusinessTrackerPostgreSqlContainer.Connection;
    
    private Task<Respawner> _respawner = null!;
  
    public override IServiceCollection RegisterDatabase(IServiceCollection services)
    {
        services.AddDbContext<BusinessTrackerDbContext>(x => x.UseNpgsql(_dataSource));
        services.AddScoped<IBusinessTrackerDbContext, BusinessTrackerDbContext>();
        // services.RemoveAll<BusinessTrackerDbContext>().AddScoped<BusinessTrackerDbContext>(sp =>
        // {
        //     var options = new RegionDbContextOptions(_dataSource);
        //     var contextOptions = sp.GetRequiredService<DbContextOptions<BusinessTrackerDbContext>>();
        //
        //     return new BusinessTrackerDbContext(options, contextOptions);
        // });
        // services.AddScoped<IApplicationPlanningDbContext, BusinessTrackerDbContext>(sp => sp.GetRequiredService<BusinessTrackerDbContext>());
        // services.AddScoped<IApplicationPlanningDbContextFactory>(x =>
        // {
        //     var db = x.GetRequiredService<BusinessTrackerDbContext>();
        //     
        //     var factory = Substitute.For<IApplicationPlanningDbContextFactory>();
        //     factory.CreateForRegion(default).ReturnsForAnyArgs(db);
        //     factory.CreateForCurrentRegion().ReturnsForAnyArgs(db);
        //     factory.CreateWithoutRegion().ReturnsForAnyArgs(db);
        //     
        //     return factory;
        // });

        return services;
    }

    public override async Task InitializeAsync(IServiceProvider sp)
    {
        if (_initialized)
            return;

        try
        {
            await ApplyMigrations(sp);
        }
        catch (Exception e)
        {
            try
            {
                await BusinessTrackerPostgreSqlContainer.ReinitializeAsync();
                await ApplyMigrations(sp);
            }
            catch (Exception exception)
            {
                await BusinessTrackerPostgreSqlContainer.KillContainerAsync();
                throw new AggregateException("Failed to apply migrations. BusinessTracker database container has been killed." +
                                       "Please check the inner exception for details and restart the tests.",
                    e, exception);
            }
        }

        _respawner = InitializeRespawner();
        _initialized = true;
    }

    private static async Task ApplyMigrations(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var dbMigration = Task.Run(() =>
        {
            var context = scope.ServiceProvider.GetRequiredService<BusinessTrackerDbContext>();

            if (BusinessTrackerPostgreSqlContainer.IsMigrationApplied(context).GetAwaiter().GetResult())
                return;

            MigrateContext(context);

            BusinessTrackerPostgreSqlContainer.MarkMigrationAsApplied(context).GetAwaiter().GetResult();
        });

        await dbMigration;
        
        await BusinessTrackerPostgreSqlContainer.ReloadDataSource();
    }

    private static void MigrateContext(DbContext context)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        {
            var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();
            if (!databaseCreator.Exists())
            {
                databaseCreator.Create();
            }

            context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS citext;");
        }

        var pendingMigrations = context.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Count > 0)
        {
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            var migrator = context.GetService<IMigrator>();
            var migrationSql = migrator.GenerateScript(
                fromMigration: appliedMigrations.LastOrDefault(),
                toMigration: pendingMigrations.Last(),
                options: Transaction.Current is not null
                    ? MigrationsSqlGenerationOptions.NoTransactions // Ambient transaction is present, omit creating a new one
                    : MigrationsSqlGenerationOptions.Default
            );

            context.Database.ExecuteSqlRaw(migrationSql);
        }
    }
    
    private async Task<Respawner> InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        return await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [
                "public",
                BusinessTrackerDbContext.SchemaName,
                BusinessTrackerDbContext.StorageSchema,
                BusinessTrackerDbContext.SalesSchema
            ],
            TablesToIgnore = [
                new Table(BusinessTrackerDbContext.SchemaName, BusinessTrackerDbContext.MigrationHistoryTableName)
            ]
        });
    }

    public override async Task ResetDatabaseAsync(IServiceProvider sp)
    {
        await (await _respawner).ResetAsync(_dbConnection);
        await ReseedDatabaseAsync(sp);
    }
    
    private async Task ReseedDatabaseAsync(IServiceProvider sp)
    {
        // Use when needed, for now just return
        return;
        
        var database = sp.GetRequiredService<BusinessTrackerDbContext>();
        
        database.ChangeTracker.Clear();
        
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
    }

    public override ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public override void Dispose()
    { 
    }
}
