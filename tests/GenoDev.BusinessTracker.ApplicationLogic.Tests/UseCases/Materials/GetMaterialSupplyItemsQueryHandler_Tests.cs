using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetMaterialSupplyItemsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetMaterialSupplyItemsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnSupplyItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "Material A", ean: "123");
            var material2 = db.Arrange_Material(name: "Material B", ean: "456");

            db.Arrange_MaterialSupplyItem(supply, material1, setsAmount: 2, unitsInSet: 1, setNetPrice: 10, setGrossPrice: 12.3m);
            db.Arrange_MaterialSupplyItem(supply, material2, setsAmount: 5, unitsInSet: 1, setNetPrice: 20, setGrossPrice: 24.6m);
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        
        var item1 = result.Items.First(x => x.MaterialName == "Material A");
        item1.MaterialId.Should().NotBeEmpty();
        item1.Ean.Should().Be("123");
        item1.SetsAmount.Should().Be(2);
        item1.TotalAmount.Should().Be(2); // unitsInSet is 1
        item1.SetNetPrice.Should().Be(10);
        item1.TotalNetPrice.Should().Be(20);
        item1.SetGrossPrice.Should().Be(12.3m);
        item1.TotalGrossPrice.Should().Be(24.6m);

        var item2 = result.Items.First(x => x.MaterialName == "Material B");
        item2.MaterialId.Should().NotBeEmpty();
        item2.Ean.Should().Be("456");
        item2.SetsAmount.Should().Be(5);
        item2.TotalAmount.Should().Be(5);
        item2.SetNetPrice.Should().Be(20);
        item2.TotalNetPrice.Should().Be(100);
        item2.SetGrossPrice.Should().Be(24.6m);
        item2.TotalGrossPrice.Should().Be(123.0m);
    }

    [Fact]
    public async Task Handle_WithTotalAmountFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "A");
            var material2 = db.Arrange_Material(name: "B");

            db.Arrange_MaterialSupplyItem(supply, material1, setsAmount: 2, unitsInSet: 10); // Total: 20
            db.Arrange_MaterialSupplyItem(supply, material2, setsAmount: 3, unitsInSet: 10); // Total: 30
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId, TotalAmountFilter: 25, TotalAmountOperator: GenoDev.BusinessTracker.Domain.Enums.NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().MaterialName.Should().Be("B");
        result.Items.First().TotalAmount.Should().Be(30);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "Specific Material");
            var material2 = db.Arrange_Material(name: "Other");

            db.Arrange_MaterialSupplyItem(supply, material1);
            db.Arrange_MaterialSupplyItem(supply, material2);
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId, SearchTerm: "Specific");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().MaterialName.Should().Be("Specific Material");
    }

    [Fact]
    public async Task Handle_WithSorting_ShouldSortItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "A");
            var material2 = db.Arrange_Material(name: "B");

            db.Arrange_MaterialSupplyItem(supply, material1);
            db.Arrange_MaterialSupplyItem(supply, material2);
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId, SortColumn: "MaterialName", SortDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].MaterialName.Should().Be("B");
        result.Items[1].MaterialName.Should().Be("A");
    }

    [Fact]
    public async Task Handle_WithSetsAmountFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "A");
            var material2 = db.Arrange_Material(name: "B");

            db.Arrange_MaterialSupplyItem(supply, material1, setsAmount: 10);
            db.Arrange_MaterialSupplyItem(supply, material2, setsAmount: 20);
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId, SetsAmountFilter: 15, SetsAmountOperator: GenoDev.BusinessTracker.Domain.Enums.NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().MaterialName.Should().Be("B");
    }

    [Fact]
    public async Task Handle_WithTotalNetPriceFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material1 = db.Arrange_Material(name: "A");
            var material2 = db.Arrange_Material(name: "B");

            db.Arrange_MaterialSupplyItem(supply, material1, setsAmount: 2, setNetPrice: 10); // Total: 20
            db.Arrange_MaterialSupplyItem(supply, material2, setsAmount: 3, setNetPrice: 10); // Total: 30
            return supply.Id;
        });

        var query = new GetMaterialSupplyItemsQuery(supplyId, TotalNetPriceFilter: 25, TotalNetPriceOperator: GenoDev.BusinessTracker.Domain.Enums.NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().MaterialName.Should().Be("B");
    }
}
