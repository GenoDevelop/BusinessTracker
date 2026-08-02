using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrderPackingMaterials;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.GetOrderPackingMaterials;

public class GetOrderPackingMaterialsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetOrderPackingMaterialsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllFieldsCorrectly()
    {
        // Arrange
        var orderId = Guid.Empty;
        var opmId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var pm = db.Arrange_PackingMaterial(name: "Box A", ean: "123", manufacturerCode: "MC-1", unit: "pcs");
            var order = db.Arrange_Order();
            var opm = db.Arrange_OrderPackingMaterial(order, pm, amount: 10.5);
            orderId = order.Id;
            opmId = opm.Id;
        });

        var query = new GetOrderPackingMaterialsQuery(orderId, 0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(opmId);
        item.Name.Should().Be("Box A");
        item.Ean.Should().Be("123");
        item.ManufacturerCode.Should().Be("MC-1");
        item.Amount.Should().Be(10.5);
        item.Unit.Should().Be("pcs");
    }

    [Fact]
    public async Task Handle_ShouldFilterByOrderId()
    {
        // Arrange
        var order1Id = Guid.Empty;
        var order2Id = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var pm = db.Arrange_PackingMaterial();
            var order1 = db.Arrange_Order();
            var order2 = db.Arrange_Order();
            db.Arrange_OrderPackingMaterial(order1, pm);
            db.Arrange_OrderPackingMaterial(order2, pm);
            order1Id = order1.Id;
            order2Id = order2.Id;
        });

        var query = new GetOrderPackingMaterialsQuery(order1Id, 0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldFilterByTextProperties()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order();
            var pm1 = db.Arrange_PackingMaterial(name: "Target Name", ean: "111", manufacturerCode: "AAA");
            var pm2 = db.Arrange_PackingMaterial(name: "Other", ean: "222", manufacturerCode: "BBB");
            db.Arrange_OrderPackingMaterial(order, pm1);
            db.Arrange_OrderPackingMaterial(order, pm2);
            orderId = order.Id;
        });

        // Test Name Filter
        var resultName = await Sut.Handle(new GetOrderPackingMaterialsQuery(orderId, 0, 10, NameFilter: "Target"), CancellationToken.None);
        resultName.Items.Should().HaveCount(1);
        resultName.Items[0].Name.Should().Be("Target Name");

        // Test EAN Filter
        var resultEan = await Sut.Handle(new GetOrderPackingMaterialsQuery(orderId, 0, 10, EanFilter: "111"), CancellationToken.None);
        resultEan.Items.Should().HaveCount(1);
        resultEan.Items[0].Ean.Should().Be("111");

        // Test Manufacturer Code Filter
        var resultMc = await Sut.Handle(new GetOrderPackingMaterialsQuery(orderId, 0, 10, ManufacturerCodeFilter: "AAA"), CancellationToken.None);
        resultMc.Items.Should().HaveCount(1);
        resultMc.Items[0].ManufacturerCode.Should().Be("AAA");
    }

    [Fact]
    public async Task Handle_ShouldFilterByAmount()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order();
            var pm = db.Arrange_PackingMaterial();
            db.Arrange_OrderPackingMaterial(order, pm, amount: 5);
            db.Arrange_OrderPackingMaterial(order, pm, amount: 15);
            orderId = order.Id;
        });

        var query = new GetOrderPackingMaterialsQuery(orderId, 0, 10, AmountOperator: NumericOperator.GreaterThan, AmountValue: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Amount.Should().Be(15);
    }

    [Fact]
    public async Task Handle_ShouldSortCorrectly()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order();
            var pm1 = db.Arrange_PackingMaterial(name: "A", ean: "333", manufacturerCode: "Z");
            var pm2 = db.Arrange_PackingMaterial(name: "Z", ean: "111", manufacturerCode: "A");
            db.Arrange_OrderPackingMaterial(order, pm1, amount: 5);
            db.Arrange_OrderPackingMaterial(order, pm2, amount: 15);
            orderId = order.Id;
        });

        // Sort by Name Desc
        var resName = await Sut.Handle(new GetOrderPackingMaterialsQuery(orderId, 0, 10, SortBy: OrderPackingMaterialSortBy.Name, IsDescending: true), CancellationToken.None);
        resName.Items[0].Name.Should().Be("Z");

        // Sort by Amount Asc
        var resAmount = await Sut.Handle(new GetOrderPackingMaterialsQuery(orderId, 0, 10, SortBy: OrderPackingMaterialSortBy.Amount, IsDescending: false), CancellationToken.None);
        resAmount.Items[0].Amount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        var orderId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order();
            var pm = db.Arrange_PackingMaterial();
            for (int i = 0; i < 15; i++)
            {
                db.Arrange_OrderPackingMaterial(order, pm);
            }
            orderId = order.Id;
        });

        var query = new GetOrderPackingMaterialsQuery(orderId, PageIndex: 1, PageSize: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
    }
}
