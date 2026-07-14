using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired(false);
        builder.Property(x => x.Ean).IsRequired(false);
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Unit).IsRequired(false);
        builder.Property(x => x.Amount).IsRequired();

        builder.HasIndex(x => x.Ean)
            .IsUnique()
            .HasFilter("\"ean\" IS NOT NULL");

        builder.HasMany(x => x.MaterialSupplyItems)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId);

        builder.HasMany(x => x.ProductRecipeMaterials)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId);

        builder.HasMany(x => x.ProductionMaterials)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId);
    }
}
