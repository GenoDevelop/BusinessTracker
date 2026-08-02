using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales.CreateOrder;

public class CreateOrder_Tests : BusinessTrackerUnitTestsBase<CreateOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderAndClientDetails()
    {
        // Arrange
        var orderData = new OrderData(
            Description: "Test Order Description",
            OrderDate: new DateTime(2026, 7, 31),
            OrderIdentifier: "ORD-123",
            PaymentIdentifier: "PAY-456",
            TrackingNumber: "TRACK-789",
            Carrier: Carrier.Dhl,
            CompanyOrder: true,
            OrderSource: "Online Store",
            ShippingNetCost: 10.00m,
            ShippingGrossCost: 12.30m,
            ShippingNetClientPrice: 15.00m,
            ShippingGrossClientPrice: 18.45m
        );

        var clientData = new ClientData(
            ClientName: "John Doe",
            Street: "Main St 1",
            PostCode: "12-345",
            City: "New York",
            Email: "john@example.com",
            Phone: "555-1234",
            ClientDescription: "Regular client"
        );

        var command = new CreateOrderCommand(orderData, clientData);

        // Act
        var orderId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var order = db.Orders
                .Include(o => o.ClientDetails)
                .FirstOrDefault(o => o.Id == orderId);

            order.Should().NotBeNull();
            order!.Description.Should().Be(orderData.Description);
            order.OrderDate.Should().Be(orderData.OrderDate);
            order.OrderIdentifier.Should().Be(orderData.OrderIdentifier);
            order.PaymentIdentifier.Should().Be(orderData.PaymentIdentifier);
            order.TrackingNumber.Should().Be(orderData.TrackingNumber);
            order.Carrier.Should().Be(orderData.Carrier);
            order.Status.Should().Be(OrderStatus.New);
            order.CompanyOrder.Should().Be(orderData.CompanyOrder);
            order.OrderSource.Should().Be(orderData.OrderSource);
            order.ShippingNetCost.Should().Be(orderData.ShippingNetCost);
            order.ShippingGrossCost.Should().Be(orderData.ShippingGrossCost);
            order.ShippingNetClientPrice.Should().Be(orderData.ShippingNetClientPrice);
            order.ShippingGrossClientPrice.Should().Be(orderData.ShippingGrossClientPrice);

            order.ClientDetails.Should().NotBeNull();
            order.ClientDetails!.ClientName.Should().Be(clientData.ClientName);
            order.ClientDetails.Street.Should().Be(clientData.Street);
            order.ClientDetails.PostCode.Should().Be(clientData.PostCode);
            order.ClientDetails.City.Should().Be(clientData.City);
            order.ClientDetails.Email.Should().Be(clientData.Email);
            order.ClientDetails.Phone.Should().Be(clientData.Phone);
            order.ClientDetails.Description.Should().Be(clientData.ClientDescription);
            order.ClientDetails.OrderId.Should().Be(orderId);
        });
    }
}
