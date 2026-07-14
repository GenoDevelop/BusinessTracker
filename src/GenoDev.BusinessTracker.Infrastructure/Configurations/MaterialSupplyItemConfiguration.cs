using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class MaterialSupplyItemConfiguration : IEntityTypeConfiguration<MaterialSupplyItem>
{
    public void Configure(EntityTypeBuilder<MaterialSupplyItem> builder)
    {
        builder.ToTable("material_supply_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasOne(x => x.MaterialSupply)
            .WithMany(x => x.MaterialSupplyItems)
            .HasForeignKey(x => x.MaterialSupplyId);

        builder.HasOne(x => x.Material)
            .WithMany(x => x.MaterialSupplyItems)
            .HasForeignKey(x => x.MaterialId);
    }
}
