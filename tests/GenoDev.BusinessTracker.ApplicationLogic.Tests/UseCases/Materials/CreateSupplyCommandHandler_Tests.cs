using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class CreateSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<CreateSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateSupplyWithStatusNew()
    {
        // Arrange
        var supplier = Arrange_BusinessTrackerDatabase(db => db.Arrange_Supplier());
        var orderDate = DateTime.UtcNow;
        var description = "Test Description";
        var invoiceNo = "INV/2025/001";

        var command = new CreateSupplyCommand(
            supplier.Id,
            orderDate,
            description,
            invoiceNo,
            10.50m,
            12.91m);

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().NotBeEmpty();
        
        Assert_BusinessTrackerDatabase(db =>
        {
            var supply = db.Supplies.Include(x => x.Supplier).FirstOrDefault(x => x.Id == resultId);
            supply.Should().NotBeNull();
            supply!.SupplierId.Should().Be(supplier.Id);
            supply.OrderDate.Should().BeCloseTo(orderDate, TimeSpan.FromMilliseconds(1));
            supply.Description.Should().Be(description);
            supply.InvoiceNo.Should().Be(invoiceNo);
            supply.ShippingNetPrice.Should().Be(10.50m);
            supply.ShippingGrossPrice.Should().Be(12.91m);
            supply.Status.Should().Be(MaterialSupplyStatus.New);
        });
    }
}
