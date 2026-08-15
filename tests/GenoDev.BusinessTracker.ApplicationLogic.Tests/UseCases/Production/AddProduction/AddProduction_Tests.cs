using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.AddProduction;

public class AddProduction_Tests : BusinessTrackerUnitTestsBase<AddProductionCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldCreateProductionAndUpdateInventory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product 1", totalAmount: 10);
            var m1 = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m1, id: variantId1, name: "Variant 1", companyAmount: 100);
            var m2 = db.Arrange_Material(name: "Material 2");
            db.Arrange_MaterialVariant(m2, id: variantId2, name: "Variant 2", companyAmount: 200);
        });

        var usedMaterials = new List<MaterialVariantUsageDto>
        {
            new(null, variantId1, 4),
            new(null, variantId2, 6)
        };

        var command = new AddProductionCommand(
            ProductId: productId,
            Amount: 5,
            Description: "Test Production",
            ProductionDate: DateTime.Now,
            UsedMaterials: usedMaterials
        );

        // Act
        var createdProductionId = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var production = db.Productions
                .Include(p => p.ProductionMaterials)
                .FirstOrDefault(p => p.Id == createdProductionId);

            production.Should().NotBeNull();
            production!.ProductId.Should().Be(productId);
            production.Amount.Should().Be(5);
            production.Description.Should().Be("Test Production");
            production.ProductionMaterials.Should().HaveCount(2);

            var product = db.Products.Find(productId);
            product!.TotalAmount.Should().Be(15); // 10 + 5

            var variant1 = db.MaterialVariants.Find(variantId1);
            variant1!.TotalCompanyAmount.Should().Be(100); // Should remain unchanged
            variant1.TotalUsedAmount.Should().Be(20); // 4 * 5 = 20

            var variant2 = db.MaterialVariants.Find(variantId2);
            variant2!.TotalCompanyAmount.Should().Be(200); // Should remain unchanged
            variant2.TotalUsedAmount.Should().Be(30); // 6 * 5 = 30
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowExceptionWhenDuplicateMaterialVariantsAreUsed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product 1", totalAmount: 10);
            var m = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m, id: variantId, name: "Variant 1", companyAmount: 100);
        });

        // Scenario: same material used twice
        var usedMaterials = new List<MaterialVariantUsageDto>
        {
            new(null, variantId, 10),
            new(null, variantId, 25)
        };

        var command = new AddProductionCommand(
            ProductId: productId,
            Amount: 2,
            Description: "Duplicate Material Test",
            ProductionDate: DateTime.Now,
            UsedMaterials: usedMaterials
        );

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.Message == "Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.");
    }
}
