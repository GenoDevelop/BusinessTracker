using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers", "storage");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SupplierName)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.HasMany(x => x.ProductSupplies)
            .WithOne(x => x.Supplier)
            .HasForeignKey(x => x.SupplierId);
    }
}
