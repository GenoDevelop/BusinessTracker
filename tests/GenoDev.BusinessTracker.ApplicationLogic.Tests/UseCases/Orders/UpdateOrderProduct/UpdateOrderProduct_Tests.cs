using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.UpdateOrderProduct;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.UpdateOrderProduct;

public class UpdateOrderProduct_Tests : BusinessTrackerUnitTestsBase<UpdateOrderProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldUpdateOrderProductAndAdjustStock()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var product = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product());
        var initialAssignedAmount = 5;
        var orderProduct = Arrange_BusinessTrackerDatabase(db => 
            db.Arrange_OrderProduct(order, product, assignedAmount: initialAssignedAmount));
        
        var initialStock = product.TotalAmount;
        var initialSold = product.TotalSoldAmount;
        var newAssignedAmount = 10;
        
        var command = new UpdateOrderProductCommand(
            OrderProductId: orderProduct.Id,
            ProductId: product.Id,
            OrderedAmount: 20,
            AssignedAmount: newAssignedAmount,
            UnitNetPrice: 200.00m,
            UnitGrossPrice: 246.00m
        );

        // Act
        // We need to use the same scope for SUT and database operations to see the changes if they are in the same transaction,
        // but here they should be persisted by SaveChangesAsync in the handler.
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            // Re-fetch everything to avoid stale data
            var updatedOrderProduct = db.OrderProducts.AsNoTracking().FirstOrDefault(x => x.Id == orderProduct.Id);
            updatedOrderProduct.Should().NotBeNull();
            updatedOrderProduct!.OrderedAmount.Should().Be(command.OrderedAmount);
            updatedOrderProduct.AssignedAmount.Should().Be(command.AssignedAmount);
            updatedOrderProduct.UnitNetPrice.Should().Be(command.UnitNetPrice);
            updatedOrderProduct.UnitGrossPrice.Should().Be(command.UnitGrossPrice);

            var updatedProduct = db.Products.AsNoTracking().FirstOrDefault(x => x.Id == product.Id);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.TotalAmount.Should().Be(initialStock);
            updatedProduct.TotalSoldAmount.Should().Be(initialSold + (newAssignedAmount - initialAssignedAmount));
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderProductDoesNotExist()
    {
        // Arrange
        var command = new UpdateOrderProductCommand(
            OrderProductId: Guid.NewGuid(),
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

    [Fact]
    public async Task Handle_ShouldUpdateProductAndAdjustStock_WhenProductIsChanged()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var oldProduct = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product());
        var newProduct = Arrange_BusinessTrackerDatabase(db => db.Arrange_Product());
        
        var initialAssignedAmount = 5;
        var orderProduct = Arrange_BusinessTrackerDatabase(db => 
            db.Arrange_OrderProduct(order, oldProduct, assignedAmount: initialAssignedAmount));
        
        var oldProductInitialSold = oldProduct.TotalSoldAmount;
        var newProductInitialSold = newProduct.TotalSoldAmount;
        var newAssignedAmount = 10;
        
        var command = new UpdateOrderProductCommand(
            OrderProductId: orderProduct.Id,
            ProductId: newProduct.Id,
            OrderedAmount: 20,
            AssignedAmount: newAssignedAmount,
            UnitNetPrice: 200.00m,
            UnitGrossPrice: 246.00m
        );

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var updatedOrderProduct = db.OrderProducts.AsNoTracking().FirstOrDefault(x => x.Id == orderProduct.Id);
            updatedOrderProduct.Should().NotBeNull();
            updatedOrderProduct!.ProductId.Should().Be(newProduct.Id);
            updatedOrderProduct.AssignedAmount.Should().Be(newAssignedAmount);

            var oldProductUpdated = db.Products.AsNoTracking().FirstOrDefault(x => x.Id == oldProduct.Id);
            oldProductUpdated.Should().NotBeNull();
            // Reverted: oldSold - initialAssignedAmount
            oldProductUpdated!.TotalSoldAmount.Should().Be(oldProductInitialSold - initialAssignedAmount);

            var newProductUpdated = db.Products.AsNoTracking().FirstOrDefault(x => x.Id == newProduct.Id);
            newProductUpdated.Should().NotBeNull();
            // Applied: newSold + newAssignedAmount
            newProductUpdated!.TotalSoldAmount.Should().Be(newProductInitialSold + newAssignedAmount);
        });
    }
}
