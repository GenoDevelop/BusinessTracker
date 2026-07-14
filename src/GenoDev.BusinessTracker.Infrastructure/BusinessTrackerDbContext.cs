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

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialSupply> MaterialSupplies { get; set; }
    public DbSet<MaterialSupplyItem> MaterialSupplyItems { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductRecipe> ProductRecipes { get; set; }
    public DbSet<ProductRecipeMaterial> ProductRecipeMaterials { get; set; }
    public DbSet<Production> Productions { get; set; }
    public DbSet<ProductionMaterial> ProductionMaterials { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
    
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