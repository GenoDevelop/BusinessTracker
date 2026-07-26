using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductionMaterialConfiguration : IEntityTypeConfiguration<ProductionMaterial>
{
    public void Configure(EntityTypeBuilder<ProductionMaterial> builder)
    {
        builder.ToTable("production_materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UsedAmount).IsRequired();

        builder.HasOne(x => x.Production)
            .WithMany(x => x.ProductionMaterials)
            .HasForeignKey(x => x.ProductionId);

        builder.HasOne(x => x.MaterialVariant)
            .WithMany(x => x.ProductionMaterials)
            .HasForeignKey(x => x.MaterialVariantId);
    }
}
