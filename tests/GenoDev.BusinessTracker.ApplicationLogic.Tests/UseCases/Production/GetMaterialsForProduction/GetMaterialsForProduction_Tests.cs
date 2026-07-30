using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialsForProduction;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetMaterialsForProduction;

public class GetMaterialsForProduction_Tests : BusinessTrackerUnitTestsBase<GetMaterialsForProductionQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnMaterialsWithAtLeastOneNonExcludedVariant()
    {
        // Arrange
        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();
        var variantId3 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m1 = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m1, id: variantId1, name: "V1");
            db.Arrange_MaterialVariant(m1, id: variantId2, name: "V2");

            var m2 = db.Arrange_Material(name: "Material 2");
            db.Arrange_MaterialVariant(m2, id: variantId3, name: "V3");
        });

        // Scenario: Exclude variantId1. Material 1 should still be returned because variantId2 is not excluded.
        // Material 2 should be returned because variantId3 is not excluded.
        var query = new GetMaterialsForProductionQuery(new[] { variantId1 });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().Contain(new[] { "Material 1", "Material 2" });
    }

    [Fact]
    public async Task Handle_ShouldExcludeMaterialIfAllItsVariantsAreExcluded()
    {
        // Arrange
        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var m1 = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m1, id: variantId1, name: "V1");

            var m2 = db.Arrange_Material(name: "Material 2");
            db.Arrange_MaterialVariant(m2, id: variantId2, name: "V2");
        });

        // Scenario: Exclude variantId1. Material 1 should NOT be returned.
        var query = new GetMaterialsForProductionQuery(new[] { variantId1 });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Material 2");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var m1 = db.Arrange_Material(name: "Apple");
            db.Arrange_MaterialVariant(m1, name: "V1");

            var m2 = db.Arrange_Material(name: "Banana");
            db.Arrange_MaterialVariant(m2, name: "V2");
        });

        var query = new GetMaterialsForProductionQuery(Enumerable.Empty<Guid>(), SearchTerm: "App");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Apple");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyWhenNoMaterialsMatch()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var m1 = db.Arrange_Material(name: "Material 1");
            db.Arrange_MaterialVariant(m1, id: variantId, name: "V1");
        });

        var query = new GetMaterialsForProductionQuery(new[] { variantId });

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
