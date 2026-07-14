using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IBusinessTrackerDbContext
{
    DbSet<Supplier> Suppliers { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialSupply> MaterialSupplies { get; }
    DbSet<MaterialSupplyItem> MaterialSupplyItems { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductRecipe> ProductRecipes { get; }
    DbSet<ProductRecipeMaterial> ProductRecipeMaterials { get; }
    DbSet<Production> Productions { get; }
    DbSet<ProductionMaterial> ProductionMaterials { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderProduct> OrderProducts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}