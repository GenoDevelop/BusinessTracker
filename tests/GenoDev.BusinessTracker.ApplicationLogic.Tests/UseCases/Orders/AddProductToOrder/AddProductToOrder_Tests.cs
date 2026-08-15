using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddProductToOrder;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.AddProductToOrder;

public class AddProductToOrder_Tests : BusinessTrackerUnitTestsBase<AddProductToOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldAddProductToOrderAndAdjustStock()
    {
        // Arrange
        var product = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product());
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var initialAmount = product.TotalAmount;
        var initialSoldAmount = product.TotalSoldAmount;
        
        var command = new AddProductToOrderCommand(
            OrderId: order.Id,
            ProductId: product.Id,
            OrderedAmount: 10,
            AssignedAmount: 5,
            UnitNetPrice: 100.00m,
            UnitGrossPrice: 123.00m
        );

        // Act
        var createdOrderProductId = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var op = db.OrderProducts.FirstOrDefault(x => x.Id == createdOrderProductId);
            op.Should().NotBeNull();
            op!.OrderId.Should().Be(order.Id);
            op.ProductId.Should().Be(product.Id);
            op.OrderedAmount.Should().Be(command.OrderedAmount);
            op.AssignedAmount.Should().Be(command.AssignedAmount);
            op.UnitNetPrice.Should().Be(command.UnitNetPrice);
            op.UnitGrossPrice.Should().Be(command.UnitGrossPrice);

            var updatedProduct = db.Products.Find(product.Id);
            updatedProduct!.TotalAmount.Should().Be(initialAmount);
            updatedProduct.TotalSoldAmount.Should().Be(initialSoldAmount + command.AssignedAmount);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderDoesNotExist()
    {
        // Arrange
        var command = new AddProductToOrderCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            OrderedAmount: 1,
            AssignedAmount: 1,
            UnitNetPrice: 1,
            UnitGrossPrice: 1
        );

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
