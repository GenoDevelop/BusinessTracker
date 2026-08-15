using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales.UpdateOrder;

public class UpdateOrder_Tests : BusinessTrackerUnitTestsBase<UpdateOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOrderAndClientDetails()
    {
        // Arrange
        var initialOrder = Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order(
                description: "Initial Description",
                status: OrderStatus.New
            );
            db.Arrange_ClientDetails(order, clientName: "Initial Client");
            return order;
        });

        var updateOrderData = new UpdateOrderData(
            Description: "Updated Description",
            OrderDate: new DateTime(2026, 8, 1),
            OrderIdentifier: "UPD-123",
            PaymentIdentifier: "UPAY-456",
            TrackingNumber: "UTRACK-789",
            Carrier: Carrier.InPost,
            Status: OrderStatus.Shipped,
            CompanyOrder: true,
            OrderSource: "Updated Source",
            ShippingNetCost: 20.00m,
            ShippingGrossCost: 24.60m,
            ShippingNetClientPrice: 30.00m,
            ShippingGrossClientPrice: 36.90m
        );

        var updateClientData = new UpdateClientData(
            ClientName: "Updated Client",
            Street: "Updated St 2",
            PostCode: "54-321",
            City: "Los Angeles",
            Email: "updated@example.com",
            Phone: "555-9876",
            ClientDescription: "Premium client"
        );

        var command = new UpdateOrderCommand(initialOrder.Id, updateOrderData, updateClientData);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var order = db.Orders
                .Include(o => o.ClientDetails)
                .AsEnumerable()
                .FirstOrDefault(o => o.Id == initialOrder.Id);

            order.Should().NotBeNull();
            order!.Description.Should().Be(updateOrderData.Description);
            order.OrderDate.Should().Be(updateOrderData.OrderDate);
            order.OrderIdentifier.Should().Be(updateOrderData.OrderIdentifier);
            order.PaymentIdentifier.Should().Be(updateOrderData.PaymentIdentifier);
            order.TrackingNumber.Should().Be(updateOrderData.TrackingNumber);
            order.Carrier.Should().Be(updateOrderData.Carrier);
            order.Status.Should().Be(updateOrderData.Status);
            order.CompanyOrder.Should().Be(updateOrderData.CompanyOrder);
            order.OrderSource.Should().Be(updateOrderData.OrderSource);
            order.ShippingNetCost.Should().Be(updateOrderData.ShippingNetCost);
            order.ShippingGrossCost.Should().Be(updateOrderData.ShippingGrossCost);
            order.ShippingNetClientPrice.Should().Be(updateOrderData.ShippingNetClientPrice);
            order.ShippingGrossClientPrice.Should().Be(updateOrderData.ShippingGrossClientPrice);

            order.ClientDetails.Should().NotBeNull();
            order.ClientDetails!.ClientName.Should().Be(updateClientData.ClientName);
            order.ClientDetails.Street.Should().Be(updateClientData.Street);
            order.ClientDetails.PostCode.Should().Be(updateClientData.PostCode);
            order.ClientDetails.City.Should().Be(updateClientData.City);
            order.ClientDetails.Email.Should().Be(updateClientData.Email);
            order.ClientDetails.Phone.Should().Be(updateClientData.Phone);
            order.ClientDetails.Description.Should().Be(updateClientData.ClientDescription);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderDoesNotExist()
    {
        // Arrange
        var command = new UpdateOrderCommand(
            Guid.NewGuid(),
            new UpdateOrderData(null, DateTime.Now, null, null, null, null, OrderStatus.New, false, "Source", 0, 0, 0, 0),
            new UpdateClientData(null, null, null, null, null, null, null)
        );

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
