using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", "sales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SaleTime)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.Property(x => x.SaleIdentifier)
            .IsRequired(false);

        builder.Property(x => x.PaymentIdentifier)
            .IsRequired(false);

        builder.HasMany(x => x.ProductSales)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SalesId);

        builder.HasMany(x => x.SalesCostsAdjustments)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SalesId);
    }
}
