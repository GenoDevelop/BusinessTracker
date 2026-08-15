using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.AddPackingMaterialToOrder;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.AddPackingMaterialToOrder;

public class AddPackingMaterialToOrder_Tests : BusinessTrackerUnitTestsBase<AddPackingMaterialToOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldAddPackingMaterialToOrderAndAdjustStock()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var packingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_PackingMaterial());
        
        var initialStock = packingMaterial.TotalCompanyAmount;
        var initialUsed = packingMaterial.TotalUsedAmount;
        
        var command = new AddPackingMaterialToOrderCommand(
            OrderId: order.Id,
            PackingMaterialId: packingMaterial.Id,
            Amount: 2.5
        );

        // Act
        var createdOrderPackingMaterialId = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var opm = db.OrderPackingMaterials.FirstOrDefault(x => x.Id == createdOrderPackingMaterialId);
            opm.Should().NotBeNull();
            opm!.OrderId.Should().Be(order.Id);
            opm.PackingMaterialId.Should().Be(packingMaterial.Id);
            opm.Amount.Should().Be(command.Amount);

            var updatedPackingMaterial = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == packingMaterial.Id);
            updatedPackingMaterial.Should().NotBeNull();
            updatedPackingMaterial!.TotalCompanyAmount.Should().Be(initialStock);
            updatedPackingMaterial.TotalUsedAmount.Should().Be(initialUsed + command.Amount);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderDoesNotExist()
    {
        // Arrange
        var command = new AddPackingMaterialToOrderCommand(
            OrderId: Guid.NewGuid(),
            PackingMaterialId: Guid.NewGuid(),
            Amount: 1.0
        );

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
