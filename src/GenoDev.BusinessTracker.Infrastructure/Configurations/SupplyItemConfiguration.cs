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
        builder.Property(x => x.ItemType).IsRequired();
        builder.Property(x => x.MaterialVariantId).IsRequired(false);
        builder.Property(x => x.PackingMaterialId).IsRequired(false);
        builder.Property(x => x.FixedAssetId).IsRequired(false);
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

        builder.HasOne(x => x.FixedAsset)
            .WithMany(x => x.SupplyItems)
            .HasForeignKey(x => x.FixedAssetId);

        builder.ToTable(t => t.HasCheckConstraint("CK_SupplyItem_XOR_Type",
            @"(""item_type"" = 1 AND ""material_variant_id"" IS NOT NULL AND ""packing_material_id"" IS NULL AND ""fixed_asset_id"" IS NULL) OR 
              (""item_type"" = 2 AND ""material_variant_id"" IS NULL AND ""packing_material_id"" IS NOT NULL AND ""fixed_asset_id"" IS NULL) OR 
              (""item_type"" = 3 AND ""material_variant_id"" IS NULL AND ""packing_material_id"" IS NULL AND ""fixed_asset_id"" IS NOT NULL)"));
    }
}
