using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetMaterialSuppliesQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetMaterialSuppliesQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuppliesOrderedByDateDescendingByDefault()
    {
        // Arrange
        var now = DateTime.Today;
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_MaterialSupply(orderDate: now.AddDays(-2), description: "Oldest");
            db.Arrange_MaterialSupply(orderDate: now, description: "Newest");
            db.Arrange_MaterialSupply(orderDate: now.AddDays(-1), description: "Middle");
        });

        var query = new GetMaterialSuppliesQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].OrderDate.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
        result.Items[1].OrderDate.Should().BeCloseTo(now.AddDays(-1), TimeSpan.FromMilliseconds(1));
        result.Items[2].OrderDate.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange()
    {
        // Arrange
        var now = new DateTime(2025, 2, 19, 12, 0, 0);
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_MaterialSupply(orderDate: now.AddDays(-5)); // Out (before start)
            db.Arrange_MaterialSupply(orderDate: now.AddDays(-2)); // In
            db.Arrange_MaterialSupply(orderDate: now);            // In
            db.Arrange_MaterialSupply(orderDate: now.AddDays(1));  // Out (after end)
        });

        var query = new GetMaterialSuppliesQuery(0, 10, 
            StartDate: now.AddDays(-3), 
            EndDate: now);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(x => x.OrderDate >= now.AddDays(-3) && x.OrderDate <= now).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalGrossPriceCorrectly()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            db.Arrange_MaterialSupplyItem(supply, setsAmount: 2, setGrossPrice: 10.5m); // 21.0
            db.Arrange_MaterialSupplyItem(supply, setsAmount: 3, setGrossPrice: 5.0m);  // 15.0
            // Total should be 36.0
        });

        var query = new GetMaterialSuppliesQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].TotalGrossPrice.Should().Be(36.0m);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResults()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (int i = 0; i < 15; i++)
            {
                db.Arrange_MaterialSupply(orderDate: DateTime.Today.AddHours(-i));
            }
        });

        var query = new GetMaterialSuppliesQuery(PageIndex: 1, PageSize: 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.HasNextPage.Should().BeFalse();
    }
}
