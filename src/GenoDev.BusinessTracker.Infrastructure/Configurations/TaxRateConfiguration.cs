using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("tax_rates", BusinessTrackerDbContext.SalesSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TaxRateName)
            .IsRequired();

        builder.Property(x => x.VatRate)
            .IsRequired();

        builder.Property(x => x.TaxRateValue)
            .IsRequired();

        builder.HasMany(x => x.ProductSales)
            .WithOne(x => x.TaxRate)
            .HasForeignKey(x => x.TaxRateId);
    }
}
