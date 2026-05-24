using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductQuantityAdjustmentConfiguration : IEntityTypeConfiguration<ProductQuantityAdjustment>
{
    public void Configure(EntityTypeBuilder<ProductQuantityAdjustment> builder)
    {
        builder.ToTable("product_quantity_adjustments", "storage");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductQuantityAdjustments)
            .HasForeignKey(x => x.ProductId);
    }
}
