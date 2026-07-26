using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("fixed_assets");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Ean);
        builder.Property(x => x.ManufacturerCode);
        builder.Property(x => x.Unit);
        builder.Property(x => x.Description);

        builder.Property(x => x.TotalCompanyAmount).IsRequired().HasDefaultValue(0.0);
        builder.Property(x => x.TotalPrivateAmount).IsRequired().HasDefaultValue(0.0);

        builder.HasIndex(x => x.Ean)
            .IsUnique()
            .HasFilter("\"ean\" IS NOT NULL");
        
        builder.HasMany(x => x.SupplyItems)
            .WithOne(x => x.FixedAsset)
            .HasForeignKey(x => x.FixedAssetId)
            .IsRequired(false);
    }
}
