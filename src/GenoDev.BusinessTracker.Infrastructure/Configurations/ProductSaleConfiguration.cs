using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductSaleConfiguration : IEntityTypeConfiguration<ProductSale>
{
    public void Configure(EntityTypeBuilder<ProductSale> builder)
    {
        builder.ToTable("product_sales", BusinessTrackerDbContext.SalesSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.TaxRateId)
            .IsRequired();

        builder.Property(x => x.SalesId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.SalePriceGross)
            .IsRequired();

        builder.Property(x => x.Decription)
            .IsRequired(false);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductSales)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TaxRate)
            .WithMany(x => x.ProductSales)
            .HasForeignKey(x => x.TaxRateId);

        builder.HasOne(x => x.Sale)
            .WithMany(x => x.ProductSales)
            .HasForeignKey(x => x.SalesId);
    }
}
