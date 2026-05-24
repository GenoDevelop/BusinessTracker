using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

public interface IBusinessTrackerDbContext
{
    DbSet<Product> Products { get; }
    DbSet<ProductQuantityAdjustment> ProductQuantityAdjustments { get; }
    DbSet<ProductSupply> ProductSupplies { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Sale> Sales { get; }
    DbSet<ProductSale> ProductSales { get; }
    DbSet<SalesCostsAdjustment> SalesCostsAdjustments { get; }
    DbSet<TaxRate> TaxRates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}