using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    public void Configure(EntityTypeBuilder<OrderProduct> builder)
    {
        builder.ToTable("order_products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.OrderedAmount).IsRequired();
        builder.Property(x => x.AssignedAmount).IsRequired();
        builder.Property(x => x.UnitNetPrice).IsRequired();
        builder.Property(x => x.UnitGrossPrice).IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.OrderProducts)
            .HasForeignKey(x => x.OrderId);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.OrderProducts)
            .HasForeignKey(x => x.ProductId);
    }
}
