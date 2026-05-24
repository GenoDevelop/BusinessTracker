using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductQuantityAdjustments.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.Utilities.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.ProductQuantityAdjustments;

public class GetAllProductQuantityAdjustments_Tests : BusinessTrackerUnitTestsBase<GetAllProductQuantityAdjustmentsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedAndSortedAdjustments()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Test Product" };
        var adjustment1 = new ProductQuantityAdjustment { Product = product, Quantity = 1, CreatedAt = now.AddDays(-1) };
        var adjustment2 = new ProductQuantityAdjustment { Product = product, Quantity = 2, CreatedAt = now.AddDays(-2) };
        var adjustment3 = new ProductQuantityAdjustment { Product = product, Quantity = 3, CreatedAt = now.AddDays(-3) };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.ProductQuantityAdjustments.AddRange(adjustment1, adjustment2, adjustment3);
        });

        var query = new GetAllProductQuantityAdjustmentsQuery(product.Id, Page: 0, PageSize: 2, SortBy: ProductQuantityAdjustmentSortBy.CreatedAt, IsDescending: false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Quantity.Should().Be(3); // Oldest first
        result.Items[1].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldHandleDescendingSort()
    {
        // Arrange
        using var _ = TestClock.FreezeCurrentTime();
        var now = Clock.UtcNowOffset;

        var product = new Product { ProductName = "Test Product" };
        var adjustment1 = new ProductQuantityAdjustment { Product = product, Quantity = 1, CreatedAt = now.AddDays(-1) };
        var adjustment2 = new ProductQuantityAdjustment { Product = product, Quantity = 2, CreatedAt = now.AddDays(-2) };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.ProductQuantityAdjustments.AddRange(adjustment1, adjustment2);
        });

        var query = new GetAllProductQuantityAdjustmentsQuery(product.Id, SortBy: ProductQuantityAdjustmentSortBy.CreatedAt, IsDescending: true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].Quantity.Should().Be(1); // Newest first
        result.Items[1].Quantity.Should().Be(2);
    }
}
