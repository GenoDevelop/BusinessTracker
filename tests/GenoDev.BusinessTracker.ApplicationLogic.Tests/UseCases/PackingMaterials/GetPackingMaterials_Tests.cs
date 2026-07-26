using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.PackingMaterials;

public class GetPackingMaterials_Tests : BusinessTrackerUnitTestsBase<GetPackingMaterialsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    protected override void PrepareTestData(IFixture dataGenerator)
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_PackingMaterial(name: "Box A", ean: "111", manufacturerCode: "M1", description: "Desc A", totalCompanyAmount: 10, unit: "pcs");
            db.Arrange_PackingMaterial(name: "Box B", ean: "222", manufacturerCode: "M2", description: "Desc B", totalCompanyAmount: 20, unit: "pcs");
            db.Arrange_PackingMaterial(name: "Tape", ean: "333", manufacturerCode: "M3", description: "Desc C", totalCompanyAmount: 30, unit: "m");
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        var query = new GetPackingMaterialsQuery(0, 2);

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
        var query = new GetPackingMaterialsQuery(0, 10, NameFilter: "Box");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(x => x.Name.Should().Contain("Box"));
    }

    [Fact]
    public async Task Handle_ShouldFilterByEan()
    {
        // Arrange
        var query = new GetPackingMaterialsQuery(0, 10, EanFilter: "222");

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
        var query = new GetPackingMaterialsQuery(0, 10, ManufacturerCodeFilter: "M3");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ManufacturerCode.Should().Be("M3");
    }

    [Fact]
    public async Task Handle_ShouldFilterByDescription()
    {
        // Arrange
        var query = new GetPackingMaterialsQuery(0, 10, DescriptionFilter: "Desc A");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Description.Should().Be("Desc A");
    }

    [Fact]
    public async Task Handle_ShouldSortByAmountDescending()
    {
        // Arrange
        var query = new GetPackingMaterialsQuery(0, 10, SortBy: PackingMaterialSortBy.Amount, IsDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items[0].TotalCompanyAmount.Should().Be(30);
        result.Items[1].TotalCompanyAmount.Should().Be(20);
        result.Items[2].TotalCompanyAmount.Should().Be(10);
    }

    [Theory]
    [InlineData(NumericOperator.Equal, 20, 1)]
    [InlineData(NumericOperator.GreaterThan, 15, 2)]
    [InlineData(NumericOperator.LessThan, 25, 2)]
    [InlineData(NumericOperator.GreaterThanOrEqual, 20, 2)]
    [InlineData(NumericOperator.LessThanOrEqual, 20, 2)]
    [InlineData(NumericOperator.NotEqual, 20, 2)]
    public async Task Handle_ShouldFilterByAmount(NumericOperator op, double value, int expectedCount)
    {
        // Arrange
        var query = new GetPackingMaterialsQuery(0, 10, AmountOperator: op, AmountValue: value);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(expectedCount);
    }
}
