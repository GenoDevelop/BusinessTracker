using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SalesCostsAdjustmentConfiguration : IEntityTypeConfiguration<SalesCostsAdjustment>
{
    public void Configure(EntityTypeBuilder<SalesCostsAdjustment> builder)
    {
        builder.ToTable("sales_costs_adjustments", "sales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SalesId)
            .IsRequired();

        builder.Property(x => x.CostName)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.Property(x => x.AdjustmentValueGross)
            .IsRequired();

        builder.HasOne(x => x.Sale)
            .WithMany(x => x.SalesCostsAdjustments)
            .HasForeignKey(x => x.SalesId);
    }
}
