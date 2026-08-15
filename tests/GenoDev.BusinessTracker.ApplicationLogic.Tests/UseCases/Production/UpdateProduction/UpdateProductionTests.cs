using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.UpdateProduction;

public class UpdateProductionTests : BusinessTrackerUnitTestsBase<UpdateProductionCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductionAndAdjustStockCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant1Id = Guid.NewGuid();
        var productionId = Guid.NewGuid();
        var productionMaterial1Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product", totalAmount: 100);
            var m = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m, id: variant1Id, name: "Variant 1", companyAmount: 50, totalUsedAmount: 20);

            var production = db.Arrange_Production(id: productionId, amount: 10, product: db.Products.Find(productId));
            db.Arrange_ProductionMaterial(id: productionMaterial1Id, production: production, materialVariant: db.MaterialVariants.Find(variant1Id), usedAmount: 2);
            // 2 * 10 = 20 used amount in storage
        });

        var command = new UpdateProductionCommand(
            productionId,
            15,
            "Updated Description",
            DateTime.Now,
            new List<MaterialVariantUsageDto>
            {
                new MaterialVariantUsageDto(productionMaterial1Id, variant1Id, 1) // Used 2 per item, now use 1 per item
                // Old total used = 2 * 10 = 20. New total used = 1 * 15 = 15.
                // Adjustment should be 15 - 20 = -5 (TotalUsedAmount tracks cumulative usage)
            });

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var production = db.Productions.Include(p => p.ProductionMaterials).First(x => x.Id == productionId);
            production.Amount.Should().Be(15);
            production.Description.Should().Be("Updated Description");
            production.ProductionMaterials.Should().HaveCount(1);

            var product = db.Products.First(x => x.Id == productId);
            product.TotalAmount.Should().Be(105); // 100 - 10 (old) + 15 (new) = 105

            var variant1 = db.MaterialVariants.First(x => x.Id == variant1Id);
            variant1.TotalCompanyAmount.Should().Be(50); // Should remain unchanged
            variant1.TotalUsedAmount.Should().Be(15); // 20 - 20 (old) + 15 (new) = 15
        });
    }

    [Fact]
    public async Task Handle_ShouldSupportAddingAndRemovingMaterials()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant1Id = Guid.NewGuid();
        var variant2Id = Guid.NewGuid();
        var productionId = Guid.NewGuid();
        var productionMaterial1Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product", totalAmount: 100);
            var m1 = db.Arrange_Material(name: "Material 1");
            var m2 = db.Arrange_Material(name: "Material 2");
            db.Arrange_MaterialVariant(m1, id: variant1Id, name: "Variant 1", companyAmount: 50, totalUsedAmount: 20);
            db.Arrange_MaterialVariant(m2, id: variant2Id, name: "Variant 2", companyAmount: 50, totalUsedAmount: 0);

            var production = db.Arrange_Production(id: productionId, amount: 10, product: db.Products.Find(productId));
            db.Arrange_ProductionMaterial(id: productionMaterial1Id, production: production, materialVariant: db.MaterialVariants.Find(variant1Id), usedAmount: 2);
        });

        var command = new UpdateProductionCommand(
            productionId,
            15,
            "Updated Description",
            DateTime.Now,
            new List<MaterialVariantUsageDto>
            {
                // Removed productionMaterial1Id
                new MaterialVariantUsageDto(null, variant2Id, 5) // New material, 5 per item
                // variant1: 20 (old total) -> removed -> should be 0
                // variant2: 0 (old total) -> added -> 5 * 15 = 75
            });

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var production = db.Productions.Include(p => p.ProductionMaterials).First(x => x.Id == productionId);
            production.Amount.Should().Be(15);
            production.ProductionMaterials.Should().HaveCount(1);
            production.ProductionMaterials.First().MaterialVariantId.Should().Be(variant2Id);
            production.ProductionMaterials.First().UsedAmount.Should().Be(5);

            var variant1 = db.MaterialVariants.First(x => x.Id == variant1Id);
            variant1.TotalUsedAmount.Should().Be(0); // 20 - 20 (removed) = 0

            var variant2 = db.MaterialVariants.First(x => x.Id == variant2Id);
            variant2.TotalUsedAmount.Should().Be(75); // 0 + 5 * 15 = 75
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowExceptionWhenDuplicateMaterialVariantsAreUsed()
    {
        // Arrange
        var productionId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var p = db.Arrange_Product(name: "Product");
            db.Arrange_Production(id: productionId, product: p);
            var m = db.Arrange_Material();
            db.Arrange_MaterialVariant(m, id: variantId);
        });

        var command = new UpdateProductionCommand(
            productionId,
            1,
            "Duplicate Test",
            DateTime.Now,
            new List<MaterialVariantUsageDto>
            {
                new(null, variantId, 1),
                new(null, variantId, 2)
            });

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.Message == "Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.");
    }
}
