using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetProductionMaterials;

public class GetProductionMaterialsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetProductionMaterialsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductionMaterialsWithVariantDetails()
    {
        // Arrange
        var productionId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var usedAmount = 5.5;
        var unit = "kg";
        var materialName = "Wood";
        var variantName = "Oak Plank";

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(id: materialId, name: materialName);
            var variant = db.Arrange_MaterialVariant(material: material, id: variantId, name: variantName, unit: unit);
            var production = db.Arrange_Production(id: productionId);
            db.Arrange_ProductionMaterial(production: production, materialVariant: variant, usedAmount: usedAmount);
            
            // Other production material to ignore
            db.Arrange_ProductionMaterial();
        });

        var query = new GetProductionMaterialsQuery(productionId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var item = result.First();
        item.MaterialId.Should().Be(materialId);
        item.MaterialName.Should().Be(materialName);
        item.MaterialVariantId.Should().Be(variantId);
        item.MaterialVariantName.Should().Be(variantName);
        item.UsedAmount.Should().Be(usedAmount);
        item.Unit.Should().Be(unit);
    }
}
