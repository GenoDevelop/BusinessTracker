using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.Create;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductQuantityAdjustments;

public class CreateProductQuantityAdjustment_Tests : BusinessTrackerUnitTestsBase<CreateProductQuantityAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductQuantityAdjustment_WhenValidDataProvided()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Test Product" };
        ArrangeBusinessTracker_Database(db => db.Products.Add(product));

        var command = new CreateProductQuantityAdjustmentCommand(product.Id, 10.5, "Restock");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(product.Id);
        result.Quantity.Should().Be(10.5);
        result.Description.Should().Be("Restock");
        result.CreatedAt.Should().Be(now);

        AssertBusinessTracker_Database(db =>
        {
            var entity = db.ProductQuantityAdjustments.Single(x => x.Id == result.Id);
            entity.ProductId.Should().Be(product.Id);
            entity.Quantity.Should().Be(10.5);
            entity.Description.Should().Be("Restock");
            entity.CreatedAt.Should().Be(now);
        });
    }
}
