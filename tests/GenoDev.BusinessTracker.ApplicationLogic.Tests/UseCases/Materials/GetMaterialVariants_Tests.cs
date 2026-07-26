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
            
            db.Arrange_MaterialVariant(material: material, name: "VariantA", ean: "111", manufacturerCode: "MFG1", description: "Alpha", companyAmount: 10, totalUsedAmount: 5);
            db.Arrange_MaterialVariant(material: material, name: "VariantB", ean: "222", manufacturerCode: "MFG2", description: "Beta", companyAmount: 20, totalUsedAmount: 15);
            db.Arrange_MaterialVariant(material: material, name: "VariantC", ean: "333", manufacturerCode: "MFG3", description: "Gamma", companyAmount: 60, totalUsedAmount: 25);
            
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

    [Fact]
    public async Task Handle_ShouldFilterByAmount()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, AmountOperator: NumericOperator.GreaterThan, AmountValue: 25);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("VariantC");
    }

    [Fact]
    public async Task Handle_ShouldFilterByTotalUsedAmount()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, TotalUsedAmountOperator: NumericOperator.LessThan, TotalUsedAmountValue: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("VariantA");
    }

    [Fact]
    public async Task Handle_ShouldSortByTotalUsedAmount()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(_materialId, 0, 10, SortBy: MaterialVariantSortBy.TotalUsedAmount, IsDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].Name.Should().Be("VariantC");
        result.Items[1].Name.Should().Be("VariantB");
        result.Items[2].Name.Should().Be("VariantA");
    }
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(10, 0, 5, 5, 0)]
    [InlineData(10, 0, 10, 0, 0)]
    [InlineData(10, 0, 15, -5, 0)]
    [InlineData(10, 5, 15, 0, 0)]
    [InlineData(10, 10, 15, 0, 5)]
    [InlineData(5, 5, 20, -10, 0)]
    [InlineData(0, 10, 10, 0, 0)]
    [InlineData(0, 10, 20, -10, 0)]
    public async Task Handle_ShouldReturnCorrectAmounts(
        double totalCompanyAmount, double totalPrivateAmount, double totalUsedAmount,
        double expectedCompany, double expectedPrivate)
    {
        // Arrange
        var materialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(name: "Math Test Material");
            db.Arrange_MaterialVariant(
                material: material,
                name: "MathVariant",
                companyAmount: totalCompanyAmount,
                privateAmount: totalPrivateAmount,
                totalUsedAmount: totalUsedAmount);
            return material.Id;
        });

        var query = new GetMaterialVariantsQuery(materialId, 0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].TotalCompanyAmount.Should().Be(expectedCompany);
        result.Items[0].TotalPrivateAmount.Should().Be(expectedPrivate);
    }

    [Fact]
    public async Task Handle_ShouldReturnVariantsWhenMaterialIdIsEmpty()
    {
        // Arrange
        var query = new GetMaterialVariantsQuery(Guid.Empty, 0, 100);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        // We have variants from PrepareTestData and Handle_ShouldReturnCorrectAmounts theory.
        // There should be at least variants from PrepareTestData (3 variants).
        result.Items.Count.Should().BeGreaterThanOrEqualTo(3);
        result.Items.Should().Contain(x => x.Name == "VariantA");
        result.Items.Should().Contain(x => x.Name == "VariantB");
        result.Items.Should().Contain(x => x.Name == "VariantC");
    }
}
