using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SupplyItemConfiguration : IEntityTypeConfiguration<SupplyItem>
{
    public void Configure(EntityTypeBuilder<SupplyItem> builder)
    {
        builder.ToTable("supply_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.MaterialSupplyId).IsRequired();
        builder.Property(x => x.MaterialVariantId).IsRequired(false);
        builder.Property(x => x.PackingMaterialId).IsRequired(false);
        builder.Property(x => x.SetsAmount).IsRequired();
        builder.Property(x => x.UnitsInSet).IsRequired();
        builder.Property(x => x.SetNetPrice).IsRequired();
        builder.Property(x => x.SetGrossPrice).IsRequired();
        builder.Property(x => x.PrivateSupply).IsRequired();

        builder.HasOne(x => x.Supply)
            .WithMany(x => x.SupplyItems)
            .HasForeignKey(x => x.MaterialSupplyId);

        builder.HasOne(x => x.MaterialVariant)
            .WithMany(x => x.SupplyItems)
            .HasForeignKey(x => x.MaterialVariantId);

        builder.HasOne(x => x.PackingMaterial)
            .WithMany(x => x.SupplyItems)
            .HasForeignKey(x => x.PackingMaterialId);
    }
}
