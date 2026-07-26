using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetMaterialVariants_Tests : BusinessTrackerUnitTestsBase<GetMaterialVariantsQueryHandler>
{
    private Guid _materialId;

    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    protected override void PrepareTestData(IFixture dataGenerator)
    {
        _materialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(name: "Test Material");
            
            db.Arrange_MaterialVariant(material: material, name: "VariantA", ean: "111", manufacturerCode: "MFG1", description: "Alpha");
            db.Arrange_MaterialVariant(material: material, name: "VariantB", ean: "222", manufacturerCode: "MFG2", description: "Beta");
            db.Arrange_MaterialVariant(material: material, name: "VariantC", ean: "333", manufacturerCode: "MFG3", description: "Gamma");
            
            return material.Id;
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedVariants()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 2);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldFilterByName()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, NameFilter: "VariantA");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("VariantA");
    }

    [Fact]
    public async Task Handle_ShouldFilterByEan()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, EanFilter: "222");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Ean.Should().Be("222");
    }

    [Fact]
    public async Task Handle_ShouldFilterByManufacturerCode()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, ManufacturerCodeFilter: "MFG3");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ManufacturerCode.Should().Be("MFG3");
    }

    [Fact]
    public async Task Handle_ShouldFilterByDescription()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, DescriptionFilter: "Beta");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Description.Should().Be("Beta");
    }
}
