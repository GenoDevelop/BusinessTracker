using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialVariantsForProduction;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetMaterialVariantsForProduction;

public class GetMaterialVariantsForProduction_Tests : BusinessTrackerUnitTestsBase<GetMaterialVariantsForProductionQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnVariantsForSpecificMaterial()
    {
        // Arrange
        var materialId1 = Guid.NewGuid();
        var materialId2 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m1 = db.Arrange_Material(id: materialId1, name: "Material 1");
            db.Arrange_MaterialVariant(m1, name: "V1.1");
            db.Arrange_MaterialVariant(m1, name: "V1.2");

            var m2 = db.Arrange_Material(id: materialId2, name: "Material 2");
            db.Arrange_MaterialVariant(m2, name: "V2.1");
        });

        var query = new GetMaterialVariantsForProductionQuery(materialId1, Enumerable.Empty<Guid>());

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.All(v => v.MaterialId == materialId1).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldExcludeSpecifiedVariantIds()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m = db.Arrange_Material(id: materialId, name: "Material");
            db.Arrange_MaterialVariant(m, id: variantId1, name: "V1");
            db.Arrange_MaterialVariant(m, id: variantId2, name: "V2");
        });

        var query = new GetMaterialVariantsForProductionQuery(materialId, new[] { variantId1 });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Id.Should().Be(variantId2);
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        // Arrange
        var materialId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m = db.Arrange_Material(id: materialId, name: "Material");
            db.Arrange_MaterialVariant(m, name: "Apple Variant");
            db.Arrange_MaterialVariant(m, name: "Banana Variant");
        });

        var query = new GetMaterialVariantsForProductionQuery(materialId, Enumerable.Empty<Guid>(), SearchTerm: "Apple");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Apple Variant");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyWhenAllVariantsAreExcluded()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m = db.Arrange_Material(id: materialId, name: "Material");
            db.Arrange_MaterialVariant(m, id: variantId, name: "V1");
        });

        var query = new GetMaterialVariantsForProductionQuery(materialId, new[] { variantId });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
