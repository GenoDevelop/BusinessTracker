using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.DeleteProduction;

public class DeleteProductionTests : BusinessTrackerUnitTestsBase<DeleteProductionCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductionAndAdjustStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productionId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Products.Add(new Product { Id = productId, Name = "Product", Identifier = "P1", TotalAmount = 100 });
            var material = new Material { Id = Guid.NewGuid(), Name = "Material" };
            db.Materials.Add(material);
            db.MaterialVariants.Add(new MaterialVariant { Id = variantId, MaterialId = material.Id, Name = "Variant", TotalCompanyAmount = 50, TotalUsedAmount = 15 });

            var production = new Domain.Entities.Production
            {
                Id = productionId,
                ProductId = productId,
                Amount = 20,
                ProductionDate = DateTime.Now,
                ProductionMaterials = new List<ProductionMaterial>
                {
                    new ProductionMaterial
                    {
                        Id = Guid.NewGuid(),
                        MaterialVariantId = variantId,
                        UsedAmount = 15
                    }
                }
            };
            db.Productions.Add(production);
        });

        // Act
        await Sut.Handle(new DeleteProductionCommand(productionId), default);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedProduction = db.Productions.FirstOrDefault(x => x.Id == productionId);
            deletedProduction.Should().BeNull();

            var product = db.Products.First(x => x.Id == productId);
            product.TotalAmount.Should().Be(80); // 100 - 20

            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(50); // Should remain unchanged
            variant.TotalUsedAmount.Should().Be(0); // 15 - 15
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowExceptionIfProductionNotFound()
    {
        // Act
        Func<Task> act = async () => await Sut.Handle(new DeleteProductionCommand(Guid.NewGuid()), default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
