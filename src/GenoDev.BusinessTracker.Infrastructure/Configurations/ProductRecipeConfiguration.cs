using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductRecipeConfiguration : IEntityTypeConfiguration<ProductRecipe>
{
    public void Configure(EntityTypeBuilder<ProductRecipe> builder)
    {
        builder.ToTable("product_recipes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Description).IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductRecipes)
            .HasForeignKey(x => x.ProductId);

        builder.HasMany(x => x.ProductRecipeMaterials)
            .WithOne(x => x.ProductRecipe)
            .HasForeignKey(x => x.ProductRecipeId);
    }
}
