using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ProductRecipeMaterialConfiguration : IEntityTypeConfiguration<ProductRecipeMaterial>
{
    public void Configure(EntityTypeBuilder<ProductRecipeMaterial> builder)
    {
        builder.ToTable("product_recipe_materials");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasOne(x => x.ProductRecipe)
            .WithMany(x => x.ProductRecipeMaterials)
            .HasForeignKey(x => x.ProductRecipeId);

        builder.HasOne(x => x.Material)
            .WithMany(x => x.ProductRecipeMaterials)
            .HasForeignKey(x => x.MaterialId);
    }
}
