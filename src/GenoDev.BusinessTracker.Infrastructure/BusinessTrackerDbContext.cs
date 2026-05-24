using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace GenoDev.BusinessTracker.Infrastructure;

public class BusinessTrackerDbContext(DbContextOptions<BusinessTrackerDbContext> contextOptions) : DbContext(contextOptions), IBusinessTrackerDbContext
{
    public const string SchemaName = "business_tracker";
    public const string StorageSchema = "storage";
    public const string SalesSchema = "sales";
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductQuantityAdjustment> ProductQuantityAdjustments => Set<ProductQuantityAdjustment>();
    public DbSet<ProductSupply> ProductSupplies => Set<ProductSupply>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ProductSale> ProductSales => Set<ProductSale>();
    public DbSet<SalesCostsAdjustment> SalesCostsAdjustments => Set<SalesCostsAdjustment>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.HasDefaultSchema(SchemaName);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLazyLoadingProxies()
            .UseSnakeCaseNamingConvention();
    }

    public static void ModifyOptionsBuilder(NpgsqlDbContextOptionsBuilder builder)
    {
        builder.MigrationsHistoryTable(
            MigrationHistoryTableName,
            SchemaName
        );
    }
}