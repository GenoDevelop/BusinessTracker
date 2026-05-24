using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductQuantityAdjustments;

public class UpdateProductQuantityAdjustment_Tests : BusinessTrackerUnitTestsBase<UpdateProductQuantityAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductQuantityAdjustment_WhenValidDataProvided()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Test Product" };
        var adjustment = new ProductQuantityAdjustment
        {
            Product = product,
            Quantity = 5,
            Description = "Initial",
            CreatedAt = now.AddHours(-1)
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.ProductQuantityAdjustments.Add(adjustment);
        });

        var command = new UpdateProductQuantityAdjustmentCommand(adjustment.Id, 20, "Correction");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(20);
        result.Description.Should().Be("Correction");

        AssertBusinessTracker_Database(db =>
        {
            var entity = db.ProductQuantityAdjustments.Single(x => x.Id == adjustment.Id);
            entity.Quantity.Should().Be(20);
            entity.Description.Should().Be("Correction");
        });
    }
}
