using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeleteOrder;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.DeleteOrder;

public class DeleteOrder_Tests : BusinessTrackerUnitTestsBase<DeleteOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldDeleteOrder()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db =>
        {
            var o = db.Arrange_Order();
            db.Arrange_ClientDetails(o);
            return o;
        });

        var command = new DeleteOrderCommand(order.Id);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var deletedOrder = db.Orders.FirstOrDefault(o => o.Id == order.Id);
            deletedOrder.Should().BeNull();
            
            var deletedDetails = db.ClientDetails.FirstOrDefault(cd => cd.OrderId == order.Id);
            deletedDetails.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldAdjustAmountsAndThenDeleteOrder()
    {
        // Arrange
        const int initialSoldAmount = 100;
        const double initialUsedAmount = 50.0;
        const int assignedProductAmount = 5;
        const double packingMaterialAmount = 2.5;

        var (order, product, packingMaterial) = Arrange_BusinessTrackerDatabase(db =>
        {
            var p = db.Arrange_Product(soldAmount: initialSoldAmount);
            var pm = db.Arrange_PackingMaterial(totalUsedAmount: initialUsedAmount);
            var o = db.Arrange_Order();
            db.Arrange_OrderProduct(o, p, assignedAmount: assignedProductAmount);
            db.Arrange_OrderPackingMaterial(o, pm, amount: packingMaterialAmount);
            return (o, p, pm);
        });

        var command = new DeleteOrderCommand(order.Id);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var deletedOrder = db.Orders.FirstOrDefault(o => o.Id == order.Id);
            deletedOrder.Should().BeNull();

            var updatedProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.Id == product.Id);
            updatedProduct!.TotalSoldAmount.Should().Be(initialSoldAmount - assignedProductAmount);

            var updatedPackingMaterial = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == packingMaterial.Id);
            updatedPackingMaterial!.TotalUsedAmount.Should().Be(initialUsedAmount - packingMaterialAmount);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderDoesNotExist()
    {
        // Arrange
        var command = new DeleteOrderCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
