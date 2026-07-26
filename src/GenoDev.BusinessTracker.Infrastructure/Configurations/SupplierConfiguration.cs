using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Nip).IsRequired(false).HasMaxLength(20);
        builder.Property(x => x.WebsiteUrl).IsRequired(false);
        builder.Property(x => x.Description).IsRequired(false);
        
        builder.HasIndex(x => x.Nip)
            .IsUnique()
            .HasFilter("\"nip\" IS NOT NULL");
        
        builder.HasMany(x => x.Supplies)
            .WithOne(x => x.Supplier)
            .HasForeignKey(x => x.SupplierId);
    }
}
