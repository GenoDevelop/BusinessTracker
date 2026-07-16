using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class MaterialSupplyConfiguration : IEntityTypeConfiguration<MaterialSupply>
{
    public void Configure(EntityTypeBuilder<MaterialSupply> builder)
    {
        builder.ToTable("material_supplies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderDate).IsRequired().HasColumnType("timestamp");
        
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.InvoiceNo).IsRequired(false);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.MaterialSupplies)
            .HasForeignKey(x => x.SupplierId);

        builder.HasMany(x => x.MaterialSupplyItems)
            .WithOne(x => x.MaterialSupply)
            .HasForeignKey(x => x.MaterialSupplyId);
    }
}
