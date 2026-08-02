using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.DeletePackingMaterialFromOrder;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Orders.DeletePackingMaterialFromOrder;

public class DeletePackingMaterialFromOrder_Tests : BusinessTrackerUnitTestsBase<DeletePackingMaterialFromOrderCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldDeletePackingMaterialFromOrderAndAdjustStock()
    {
        // Arrange
        var order = Arrange_BusinessTrackerDatabase(db => db.Arrange_Order());
        var packingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_PackingMaterial());
        var initialAmount = 5.0;
        var orderPackingMaterial = Arrange_BusinessTrackerDatabase(db => db.Arrange_OrderPackingMaterial(order, packingMaterial, amount: initialAmount));

        var initialStock = packingMaterial.TotalCompanyAmount;
        var initialUsed = packingMaterial.TotalUsedAmount;

        var command = new DeletePackingMaterialFromOrderCommand(orderPackingMaterial.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var opm = db.OrderPackingMaterials.FirstOrDefault(x => x.Id == orderPackingMaterial.Id);
            opm.Should().BeNull();

            var updatedPackingMaterial = db.PackingMaterials.AsNoTracking().FirstOrDefault(pm => pm.Id == packingMaterial.Id);
            updatedPackingMaterial.Should().NotBeNull();
            updatedPackingMaterial!.TotalCompanyAmount.Should().Be(initialStock);
            updatedPackingMaterial.TotalUsedAmount.Should().Be(initialUsed - initialAmount);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenOrderPackingMaterialDoesNotExist()
    {
        // Arrange
        var command = new DeletePackingMaterialFromOrderCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
