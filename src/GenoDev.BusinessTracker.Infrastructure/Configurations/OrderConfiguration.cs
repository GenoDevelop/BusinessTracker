using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderDate).IsRequired().HasColumnType("timestamp");
        
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.OrderIdentifier).IsRequired(false);
        builder.Property(x => x.PaymentIdentifier).IsRequired(false);
        builder.Property(x => x.TrackingNumber).IsRequired(false);
        builder.Property(x => x.Carrier).IsRequired(false).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.CompanyOrder).IsRequired();
        builder.Property(x => x.OrderSource).IsRequired();
        builder.Property(x => x.ShippingNetCost).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.ShippingGrossCost).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.ShippingNetClientPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.ShippingGrossClientPrice).IsRequired().HasPrecision(18, 2);

        builder.HasOne(x => x.ClientDetails)
            .WithOne(x => x.Order)
            .HasForeignKey<ClientDetails>(x => x.OrderId);

        builder.HasMany(x => x.OrderProducts)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);

        builder.HasMany(x => x.OrderPackingMaterials)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);
    }
}
