using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductQuantityAdjustments;

public class DeleteProductQuantityAdjustment_Tests : BusinessTrackerUnitTestsBase<DeleteProductQuantityAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductQuantityAdjustment_WhenItExists()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Test Product" };
        var adjustment = new ProductQuantityAdjustment
        {
            Product = product,
            Quantity = 5,
            CreatedAt = now
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.ProductQuantityAdjustments.Add(adjustment);
        });

        var command = new DeleteProductQuantityAdjustmentCommand(adjustment.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.ProductQuantityAdjustments.Any(x => x.Id == adjustment.Id).Should().BeFalse();
        });
    }
}
