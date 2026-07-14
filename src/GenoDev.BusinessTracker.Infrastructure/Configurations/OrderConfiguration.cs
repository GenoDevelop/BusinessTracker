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

        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.OrderIdentifier).IsRequired(false);
        builder.Property(x => x.PaymentIdentifier).IsRequired(false);
        builder.Property(x => x.TrackingNumber).IsRequired(false);
        builder.Property(x => x.Carrier).IsRequired(false).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();

        builder.HasMany(x => x.OrderProducts)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);
    }
}
