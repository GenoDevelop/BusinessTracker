using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class MaterialVariantConfiguration : IEntityTypeConfiguration<MaterialVariant>
{
    public void Configure(EntityTypeBuilder<MaterialVariant> builder)
    {
        builder.ToTable("material_variants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Ean).IsRequired(false);
        builder.Property(x => x.ManufacturerCode).IsRequired(false);
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Unit).IsRequired(false);
        builder.Property(x => x.TotalUsedAmount).IsRequired();
        builder.Property(x => x.CompanyAmount).IsRequired();
        builder.Property(x => x.PrivateAmount).IsRequired();

        builder.HasIndex(x => x.Ean)
            .IsUnique()
            .HasFilter("\"ean\" IS NOT NULL");

        builder.HasOne(x => x.Material)
            .WithMany(x => x.MaterialVariants)
            .HasForeignKey(x => x.MaterialId);

        builder.HasMany(x => x.SupplyItems)
            .WithOne(x => x.MaterialVariant)
            .HasForeignKey(x => x.MaterialVariantId);

        builder.HasMany(x => x.ProductionMaterials)
            .WithOne(x => x.MaterialVariant)
            .HasForeignKey(x => x.MaterialVariantId);
    }
}
