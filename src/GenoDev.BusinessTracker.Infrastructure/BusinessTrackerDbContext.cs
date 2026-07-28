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
    public DbSet<Supply> Supplies { get; set; }
    public DbSet<SupplyItem> SupplyItems { get; set; }
    public DbSet<PackingMaterial> PackingMaterials { get; set; }
    public DbSet<OrderPackingMaterial> OrderPackingMaterials { get; set; }
    public DbSet<MaterialVariant> MaterialVariants { get; set; }
    public DbSet<FixedAsset> FixedAssets { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductRecipe> ProductRecipes { get; set; }
    public DbSet<ProductRecipeMaterial> ProductRecipeMaterials { get; set; }
    public DbSet<Production> Productions { get; set; }
    public DbSet<ProductionMaterial> ProductionMaterials { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ReplaceWhitespaceWithNull();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ReplaceWhitespaceWithNull();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ReplaceWhitespaceWithNull()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var properties = entry.Metadata.GetProperties()
                .Where(p => p.ClrType == typeof(string));

            foreach (var property in properties)
            {
                if (property.IsPrimaryKey())
                {
                    continue;
                }

                var currentValue = (string?)entry.Property(property.Name).CurrentValue;
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    if (property.IsNullable)
                    {
                        entry.Property(property.Name).CurrentValue = null;
                    }
                    else if (currentValue == null)
                    {
                        entry.Property(property.Name).CurrentValue = string.Empty;
                    }
                }
            }
        }
    }
    
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