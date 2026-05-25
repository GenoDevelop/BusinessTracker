using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.DeleteAdjustment;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class DeleteSalesCostsAdjustment_Tests : BusinessTrackerUnitTestsBase<DeleteSalesCostsAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteAdjustment_WhenItExists()
    {
        // Arrange
        var sale = new Sale
        {
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Test Sale"
        };
        
        var adjustment = new SalesCostsAdjustment
        {
            Sale = sale,
            CostName = "Shipping",
            AdjustmentValueGross = 10m
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Sales.Add(sale);
            db.SalesCostsAdjustments.Add(adjustment);
        });

        var command = new DeleteSalesCostsAdjustmentCommand(adjustment.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.SalesCostsAdjustments.Any(x => x.Id == adjustment.Id).Should().BeFalse();
            db.Sales.Any(x => x.Id == sale.Id).Should().BeTrue();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenAdjustmentDoesNotExist()
    {
        // Arrange
        var command = new DeleteSalesCostsAdjustmentCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
