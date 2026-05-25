using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class UpdateSale_Tests : BusinessTrackerUnitTestsBase<UpdateSaleCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSale_WhenItExists()
    {
        // Arrange
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow.AddDays(-1),
            Description = "Old Description",
            SaleIdentifier = "Old ID",
            PaymentIdentifier = "Old Payment"
        };

        ArrangeBusinessTracker_Database(db => db.Sales.Add(sale));

        var newSaleTime = DateTimeOffset.UtcNow;
        var command = new UpdateSaleCommand(
            sale.Id,
            newSaleTime,
            "New Description",
            "New ID",
            "New Payment");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(sale.Id);
        result.SaleTime.Should().BeCloseTo(newSaleTime, TimeSpan.FromMilliseconds(1));
        result.Description.Should().Be("New Description");
        result.SaleIdentifier.Should().Be("New ID");
        result.PaymentIdentifier.Should().Be("New Payment");

        AssertBusinessTracker_Database(db =>
        {
            var updatedSale = db.Sales.Find(sale.Id);
            updatedSale!.SaleTime.Should().BeCloseTo(newSaleTime, TimeSpan.FromMilliseconds(1));
            updatedSale.Description.Should().Be("New Description");
            updatedSale.SaleIdentifier.Should().Be("New ID");
            updatedSale.PaymentIdentifier.Should().Be("New Payment");
        });
    }

    [Fact]
    public async Task Handle_ShouldUpdateSale_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow.AddDays(-1),
            Description = "Old Description",
            SaleIdentifier = "Old ID",
            PaymentIdentifier = "Old Payment"
        };

        ArrangeBusinessTracker_Database(db => db.Sales.Add(sale));

        var newSaleTime = DateTimeOffset.UtcNow;
        var command = new UpdateSaleCommand(
            sale.Id,
            newSaleTime,
            null,
            null,
            null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Description.Should().BeNull();
        result.SaleIdentifier.Should().BeNull();
        result.PaymentIdentifier.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var updatedSale = db.Sales.Find(sale.Id);
            updatedSale!.Description.Should().BeNull();
            updatedSale.SaleIdentifier.Should().BeNull();
            updatedSale.PaymentIdentifier.Should().BeNull();
        });
    }
}
