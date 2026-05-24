using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductSupplyConfiguration : IEntityTypeConfiguration<ProductSupply>
{
    public void Configure(EntityTypeBuilder<ProductSupply> builder)
    {
        builder.ToTable("product_supplies", "storage");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.BuyPriceNet)
            .IsRequired();

        builder.Property(x => x.BuyPriceGross)
            .IsRequired();

        builder.Property(x => x.BuyTime)
            .IsRequired(false);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.Property(x => x.SupplyStatus)
            .IsRequired();

        builder.Property(x => x.SupplierId)
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductSupplies)
            .HasForeignKey(x => x.ProductId);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.ProductSupplies)
            .HasForeignKey(x => x.SupplierId);
    }
}
