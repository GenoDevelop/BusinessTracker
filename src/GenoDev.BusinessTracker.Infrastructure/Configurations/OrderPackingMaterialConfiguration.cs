using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class OrderPackingMaterialConfiguration : IEntityTypeConfiguration<OrderPackingMaterial>
{
    public void Configure(EntityTypeBuilder<OrderPackingMaterial> builder)
    {
        builder.ToTable("order_packing_materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.PackingMaterialId).IsRequired();
        builder.Property(x => x.Amount).IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.OrderPackingMaterials)
            .HasForeignKey(x => x.OrderId);

        builder.HasOne(x => x.PackingMaterial)
            .WithMany(x => x.OrderPackingMaterials)
            .HasForeignKey(x => x.PackingMaterialId);
    }
}
