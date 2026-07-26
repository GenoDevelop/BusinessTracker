using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> builder)
    {
        builder.ToTable("supplies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderDate).IsRequired().HasColumnType("timestamp");
        
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.InvoiceNo).IsRequired(false);
        builder.Property(x => x.ShippingNetPrice).IsRequired();
        builder.Property(x => x.ShippingGrossPrice).IsRequired();

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Supplies)
            .HasForeignKey(x => x.SupplierId);

        builder.HasMany(x => x.SupplyItems)
            .WithOne(x => x.Supply)
            .HasForeignKey(x => x.MaterialSupplyId);
    }
}
