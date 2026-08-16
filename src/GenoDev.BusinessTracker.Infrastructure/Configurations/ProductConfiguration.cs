using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Description).IsRequired(false);
        builder.Property(x => x.Identifier).IsRequired();
        builder.HasIndex(x => x.Identifier).IsUnique();
        builder.Property(x => x.TotalAmount).IsRequired();
        builder.Property(x => x.TotalSoldAmount).IsRequired();

        builder.HasMany(x => x.ProductRecipes)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.Productions)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.OrderProducts)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.Images)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
