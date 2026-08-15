using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("stock_adjustments", BusinessTrackerDbContext.StorageSchema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ItemType).IsRequired();
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.IsPrivate).IsRequired();
        builder.Property(x => x.Date).HasColumnType("date").IsRequired();
        builder.Property(x => x.Description).IsRequired(false);

        builder.HasOne(x => x.MaterialVariant).WithMany().HasForeignKey(x => x.MaterialVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PackingMaterial).WithMany().HasForeignKey(x => x.PackingMaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FixedAsset).WithMany().HasForeignKey(x => x.FixedAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_StockAdjustment_NonZeroAmount", "\"amount\" <> 0");
            table.HasCheckConstraint("CK_StockAdjustment_ProductRules", "\"item_type\" <> 4 OR (NOT \"is_private\" AND \"amount\" = trunc(\"amount\"))");
            table.HasCheckConstraint("CK_StockAdjustment_XOR_Type", @"
(""item_type"" = 1 AND ""material_variant_id"" IS NOT NULL AND ""packing_material_id"" IS NULL AND ""fixed_asset_id"" IS NULL AND ""product_id"" IS NULL) OR
(""item_type"" = 2 AND ""material_variant_id"" IS NULL AND ""packing_material_id"" IS NOT NULL AND ""fixed_asset_id"" IS NULL AND ""product_id"" IS NULL) OR
(""item_type"" = 3 AND ""material_variant_id"" IS NULL AND ""packing_material_id"" IS NULL AND ""fixed_asset_id"" IS NOT NULL AND ""product_id"" IS NULL) OR
(""item_type"" = 4 AND ""material_variant_id"" IS NULL AND ""packing_material_id"" IS NULL AND ""fixed_asset_id"" IS NULL AND ""product_id"" IS NOT NULL)");
        });

        builder.HasIndex(x => x.Date);
    }
}
