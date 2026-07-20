using AutoFixture;
using FluentAssertions;
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
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductionAndAdjustStockCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var material1Id = Guid.NewGuid();
        var productionId = Guid.NewGuid();
        var productionMaterial1Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Products.Add(new Product { Id = productId, Name = "Product", Identifier = "P1", Amount = 100 });
            db.Materials.Add(new Material { Id = material1Id, Name = "Material 1", Amount = 50 });

            var production = new Domain.Entities.Production
            {
                Id = productionId,
                ProductId = productId,
                Amount = 10,
                ProductionDate = DateTime.Now.AddDays(-1),
                ProductionMaterials = new List<ProductionMaterial>
                {
                    new ProductionMaterial { Id = productionMaterial1Id, MaterialId = material1Id, UsedAmount = 20 }
                }
            };
            db.Productions.Add(production);
        });

        var command = new UpdateProductionCommand(
            productionId,
            15,
            "Updated Description",
            DateTime.Now,
            new List<MaterialUsageDto>
            {
                new MaterialUsageDto(productionMaterial1Id, material1Id, 10) // Used 20, now use 10 (should return 10 to stock)
            });

        // Act
        await Sut.Handle(command, default);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var production = db.Productions.Include(p => p.ProductionMaterials).First(x => x.Id == productionId);
            production.Amount.Should().Be(15);
            production.Description.Should().Be("Updated Description");
            production.ProductionMaterials.Should().HaveCount(1);

            var product = db.Products.First(x => x.Id == productId);
            product.Amount.Should().Be(105); // 100 - 10 (old) + 15 (new) = 105

            var material1 = db.Materials.First(x => x.Id == material1Id);
            material1.Amount.Should().Be(60); // 50 + 20 (old) - 10 (new) = 60
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenAddingNewMaterial()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var material1Id = Guid.NewGuid();
        var material2Id = Guid.NewGuid();
        var productionId = Guid.NewGuid();
        var productionMaterial1Id = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Products.Add(new Product { Id = productId, Name = "Product", Identifier = "P1", Amount = 100 });
            db.Materials.Add(new Material { Id = material1Id, Name = "Material 1", Amount = 50 });
            db.Materials.Add(new Material { Id = material2Id, Name = "Material 2", Amount = 50 });

            var production = new Domain.Entities.Production
            {
                Id = productionId,
                ProductId = productId,
                Amount = 10,
                ProductionDate = DateTime.Now.AddDays(-1),
                ProductionMaterials = new List<ProductionMaterial>
                {
                    new ProductionMaterial { Id = productionMaterial1Id, MaterialId = material1Id, UsedAmount = 20 }
                }
            };
            db.Productions.Add(production);
        });

        var command = new UpdateProductionCommand(
            productionId,
            15,
            "Updated Description",
            DateTime.Now,
            new List<MaterialUsageDto>
            {
                new MaterialUsageDto(productionMaterial1Id, material1Id, 10),
                new MaterialUsageDto(null, material2Id, 5) // New material
            });

        // Act & Assert
        var act = () => Sut.Handle(command, default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("New materials cannot be added to production history.");
    }
}
