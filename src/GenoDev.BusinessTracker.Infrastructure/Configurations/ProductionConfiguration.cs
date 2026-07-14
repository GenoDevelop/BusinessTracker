using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductionConfiguration : IEntityTypeConfiguration<Production>
{
    public void Configure(EntityTypeBuilder<Production> builder)
    {
        builder.ToTable("production");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Description).IsRequired(false);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Productions)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.ProductionMaterials)
            .WithOne(x => x.Production)
            .HasForeignKey(x => x.ProductionId);
    }
}
