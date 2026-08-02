using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteProductFromOrder;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.DeleteProductFromOrder;

public class DeleteProductFromOrder_Tests : BusinessTrackerUnitTestsBase<DeleteProductFromOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductFromOrderAndAdjustStock()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var product = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product());
        var assignedAmount = 5;
        var orderProduct = Arrange_BusinessTrackerDatabase(db => db.Arrange_OrderProduct(order, product, assignedAmount: assignedAmount));

        var initialStock = product.TotalAmount;
        var initialSold = product.TotalSoldAmount;

        var command = new DeleteProductFromOrderCommand(orderProduct.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var op = db.OrderProducts.FirstOrDefault(x => x.Id == orderProduct.Id);
            op.Should().BeNull();

            var updatedProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.Id == product.Id);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.TotalAmount.Should().Be(initialStock);
            updatedProduct.TotalSoldAmount.Should().Be(initialSold - assignedAmount);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderProductDoesNotExist()
    {
        // Arrange
        var command = new DeleteProductFromOrderCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
