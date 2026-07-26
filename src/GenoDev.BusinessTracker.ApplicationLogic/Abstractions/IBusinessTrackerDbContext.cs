using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IBusinessTrackerDbContext
{
    DbSet<Supplier> Suppliers { get; }
    DbSet<Material> Materials { get; }
    DbSet<Supply> Supplies { get; }
    DbSet<SupplyItem> SupplyItems { get; }
    DbSet<PackingMaterial> PackingMaterials { get; }
    DbSet<OrderPackingMaterial> OrderPackingMaterials { get; }
    DbSet<MaterialVariant> MaterialVariants { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductRecipe> ProductRecipes { get; }
    DbSet<ProductRecipeMaterial> ProductRecipeMaterials { get; }
    DbSet<Production> Productions { get; }
    DbSet<ProductionMaterial> ProductionMaterials { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderProduct> OrderProducts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}