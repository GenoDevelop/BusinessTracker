using AutoFixture;
using FluentAssertions;
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
    }

    [Fact]
    public async Task Handle_ShouldCreateProductionAndUpdateInventory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product 1", amount: 10);
            db.Arrange_Material(id: materialId1, name: "Material 1", amount: 100);
            db.Arrange_Material(id: materialId2, name: "Material 2", amount: 200);
        });

        var usedMaterials = new List<MaterialUsageDto>
        {
            new(null, materialId1, 20),
            new(null, materialId2, 30)
        };

        var command = new AddProductionCommand(
            ProductId: productId,
            Amount: 5,
            Description: "Test Production",
            ProductionDate: DateTime.Now,
            UsedMaterials: usedMaterials
        );

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var production = db.Productions
                .Include(p => p.ProductionMaterials)
                .FirstOrDefault(p => p.ProductId == productId && p.Amount == 5);

            production.Should().NotBeNull();
            production!.Description.Should().Be("Test Production");
            production.ProductionMaterials.Should().HaveCount(2);

            var product = db.Products.Find(productId);
            product!.Amount.Should().Be(15); // 10 + 5

            var material1 = db.Materials.Find(materialId1);
            material1!.Amount.Should().Be(80); // 100 - 20

            var material2 = db.Materials.Find(materialId2);
            material2!.Amount.Should().Be(170); // 200 - 30
        });
    }

    [Fact]
    public async Task Handle_ShouldHandleDuplicateMaterialUsagesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product 1", amount: 10);
            db.Arrange_Material(id: materialId, name: "Material 1", amount: 100);
        });

        // Scenario: same material used twice (e.g. from different recipe steps or manual entries)
        var usedMaterials = new List<MaterialUsageDto>
        {
            new(null, materialId, 10),
            new(null, materialId, 25)
        };

        var command = new AddProductionCommand(
            ProductId: productId,
            Amount: 2,
            Description: "Duplicate Material Test",
            ProductionDate: DateTime.Now,
            UsedMaterials: usedMaterials
        );

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var production = db.Productions
                .Include(p => p.ProductionMaterials)
                .FirstOrDefault(p => p.ProductId == productId && p.Description == "Duplicate Material Test");

            production.Should().NotBeNull();
            production!.ProductionMaterials.Should().HaveCount(2);
            production.ProductionMaterials.Sum(pm => pm.UsedAmount).Should().Be(35);

            var product = db.Products.Find(productId);
            product!.Amount.Should().Be(12); // 10 + 2

            var material = db.Materials.Find(materialId);
            material!.Amount.Should().Be(65); // 100 - (10 + 25)
        });
    }
}
