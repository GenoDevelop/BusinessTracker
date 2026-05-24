using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", BusinessTrackerDbContext.StorageSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ProductName)
            .IsRequired();

        builder.Property(x => x.EanCode)
            .IsRequired(false);

        builder.HasIndex(x => x.EanCode)
            .IsUnique()
            .HasFilter("\"ean_code\" IS NOT NULL");

        builder.HasMany(x => x.ProductSales)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.ProductSupplies)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.ProductQuantityAdjustments)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);
    }
}
