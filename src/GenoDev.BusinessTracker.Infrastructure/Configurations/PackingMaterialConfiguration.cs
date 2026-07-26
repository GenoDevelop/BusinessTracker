using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class PackingMaterialConfiguration : IEntityTypeConfiguration<PackingMaterial>
{
    public void Configure(EntityTypeBuilder<PackingMaterial> builder)
    {
        builder.ToTable("packing_materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Ean).IsRequired(false);
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Unit).IsRequired(false);
        builder.Property(x => x.ManufacturerCode).IsRequired(false);
        builder.Property(x => x.TotalUsedAmount).IsRequired();
        builder.Property(x => x.CompanyAmount).IsRequired();
        builder.Property(x => x.PrivateAmount).IsRequired();

        builder.HasIndex(x => x.Ean)
            .IsUnique()
            .HasFilter("\"ean\" IS NOT NULL");

        builder.HasMany(x => x.SupplyItems)
            .WithOne(x => x.PackingMaterial)
            .HasForeignKey(x => x.PackingMaterialId);

        builder.HasMany(x => x.OrderPackingMaterials)
            .WithOne(x => x.PackingMaterial)
            .HasForeignKey(x => x.PackingMaterialId);
    }
}
