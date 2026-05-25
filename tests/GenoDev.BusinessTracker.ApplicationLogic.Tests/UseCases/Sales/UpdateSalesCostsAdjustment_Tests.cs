using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateAdjustment;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class UpdateSalesCostsAdjustment_Tests : BusinessTrackerUnitTestsBase<UpdateSalesCostsAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSalesCostsAdjustment_WhenItExists()
    {
        // Arrange
        var sale = new Sale { SaleTime = DateTimeOffset.UtcNow };
        var adjustment = new SalesCostsAdjustment
        {
            Sale = sale,
            CostName = "Old Name",
            AdjustmentValueGross = 10m,
            Description = "Old Description"
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Sales.Add(sale);
            db.SalesCostsAdjustments.Add(adjustment);
        });

        var command = new UpdateSalesCostsAdjustmentCommand(
            adjustment.Id,
            "New Name",
            25.50m,
            "New Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(adjustment.Id);
        result.CostName.Should().Be("New Name");
        result.AdjustmentValueGross.Should().Be(25.50m);
        result.Description.Should().Be("New Description");

        AssertBusinessTracker_Database(db =>
        {
            var updated = db.SalesCostsAdjustments.Find(adjustment.Id);
            updated!.CostName.Should().Be("New Name");
            updated.AdjustmentValueGross.Should().Be(25.50m);
            updated.Description.Should().Be("New Description");
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenAdjustmentDoesNotExist()
    {
        // Arrange
        var command = new UpdateSalesCostsAdjustmentCommand(
            Guid.NewGuid(),
            "Name",
            10m,
            "Description");

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
