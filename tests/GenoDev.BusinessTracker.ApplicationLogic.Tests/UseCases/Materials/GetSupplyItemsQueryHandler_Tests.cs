using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyItems;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetSupplyItemsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetSupplyItemsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnSupplyItems_OfAllTypes()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material(name: "Material A");
            var variant = db.Arrange_MaterialVariant(material, name: "Variant A", manufacturerCode: "MC001");
            var packing = db.Arrange_PackingMaterial(name: "Packing P", manufacturerCode: "MC002");
            var asset = db.Arrange_FixedAsset();

            db.Arrange_SupplyItem(supply, materialVariant: variant, setsAmount: 2, unitsInSet: 1, setNetPrice: 10, setGrossPrice: 12.3m);
            db.Arrange_SupplyItem(supply, packingMaterial: packing, setsAmount: 5, unitsInSet: 1, setNetPrice: 20, setGrossPrice: 24.6m);
            db.Arrange_SupplyItem(supply, fixedAsset: asset, setsAmount: 1, unitsInSet: 1, setNetPrice: 100, setGrossPrice: 123m);
            return supply.Id;
        });

        var query = new GetSupplyItemsQuery(supplyId, SortColumn: SupplyItemSortColumn.ItemName);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        
        var mItem = result.Items.First(x => x.ItemType == SupplyItemType.Material);
        mItem.ItemName.Should().Be("Variant A");
        mItem.ManufacturerCode.Should().Be("MC001");

        var pItem = result.Items.First(x => x.ItemType == SupplyItemType.Packing);
        pItem.ItemName.Should().Be("Packing P");
        pItem.ManufacturerCode.Should().Be("MC002");

        var fItem = result.Items.First(x => x.ItemType == SupplyItemType.FixedAsset);
        fItem.ItemName.Should().Be("Fixed Asset");
    }

    [Fact]
    public async Task Handle_WithItemTypeFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material(name: "A");
            var variant = db.Arrange_MaterialVariant(material, name: "A");
            var asset = db.Arrange_FixedAsset();

            db.Arrange_SupplyItem(supply, materialVariant: variant);
            db.Arrange_SupplyItem(supply, fixedAsset: asset);
            return supply.Id;
        });

        var query = new GetSupplyItemsQuery(supplyId, ItemTypeFilter: [SupplyItemType.FixedAsset]);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ItemType.Should().Be(SupplyItemType.FixedAsset);
    }

    [Fact]
    public async Task Handle_WithManufacturerCodeFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material(name: "A");
            var variant1 = db.Arrange_MaterialVariant(material, name: "A", manufacturerCode: "CODE123");
            var variant2 = db.Arrange_MaterialVariant(material, name: "B", manufacturerCode: "OTHER");

            db.Arrange_SupplyItem(supply, materialVariant: variant1);
            db.Arrange_SupplyItem(supply, materialVariant: variant2);
            return supply.Id;
        });

        var query = new GetSupplyItemsQuery(supplyId, ManufacturerCodeFilter: "CODE123");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ManufacturerCode.Should().Be("CODE123");
    }

    [Fact]
    public async Task Handle_WithSorting_ShouldSortByEnum()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var m1 = db.Arrange_Material(name: "A");
            var v1 = db.Arrange_MaterialVariant(m1, name: "A");
            var m2 = db.Arrange_Material(name: "B");
            var v2 = db.Arrange_MaterialVariant(m2, name: "B");

            db.Arrange_SupplyItem(supply, materialVariant: v1);
            db.Arrange_SupplyItem(supply, materialVariant: v2);
            return supply.Id;
        });

        var query = new GetSupplyItemsQuery(supplyId, SortColumn: SupplyItemSortColumn.ItemName, SortDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].ItemName.Should().Be("B");
        result.Items[1].ItemName.Should().Be("A");
    }

    [Fact]
    public async Task Handle_WithPrivateSupplyFilter_ShouldFilterItems()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material(name: "M");
            var v1 = db.Arrange_MaterialVariant(material, name: "V1");
            var v2 = db.Arrange_MaterialVariant(material, name: "V2");

            db.Arrange_SupplyItem(supply, materialVariant: v1, privateSupply: true);
            db.Arrange_SupplyItem(supply, materialVariant: v2, privateSupply: false);
            return supply.Id;
        });

        var query = new GetSupplyItemsQuery(supplyId, PrivateSupplyFilter: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().PrivateSupply.Should().BeTrue();
    }
}
