using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.GetProductionHistory;

public class GetProductionHistoryTests : BusinessTrackerUnitTestsBase<GetProductionHistoryQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductionHistoryForProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(id: productId);
            db.Arrange_Production(product, productionDate: DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Unspecified), amount: 10, description: "Desc 1");
            db.Arrange_Production(product, productionDate: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), amount: 20, description: "Desc 2");
            db.Arrange_Production(productionDate: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), amount: 30);
        });

        var query = new GetProductionHistoryQuery(productId);

        // Act
        var result = await Sut.Handle(query, default);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.First().ProductionAmount.Should().Be(20); // Sorted by date desc
        result.Items.Last().ProductionAmount.Should().Be(10);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedProductionHistory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(id: productId);
            for (int i = 1; i <= 15; i++)
            {
                db.Arrange_Production(product, productionDate: DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(i), DateTimeKind.Unspecified), amount: i);
            }
        });

        var query = new GetProductionHistoryQuery(productId, PageIndex: 1, PageSize: 10);

        // Act
        var result = await Sut.Handle(query, default);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.HasNextPage.Should().BeFalse();
        
        // Check order (date desc, so 15 to 1)
        // Page 0 should have 15-6
        // Page 1 should have 5-1
        result.Items.First().ProductionAmount.Should().Be(5);
        result.Items.Last().ProductionAmount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldFilterProductionHistory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var baseDate = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(id: productId);
            // 1: Amount 10, Date baseDate, Description "Test A"
            db.Arrange_Production(product, productionDate: baseDate, amount: 10, description: "Test A");
            // 2: Amount 20, Date baseDate + 1 day, Description "Sample B"
            db.Arrange_Production(product, productionDate: baseDate.AddDays(1), amount: 20, description: "Sample B");
            // 3: Amount 30, Date baseDate + 2 days, Description "Test C"
            db.Arrange_Production(product, productionDate: baseDate.AddDays(2), amount: 30, description: "Test C");
        });

        // Act & Assert 1: Description
        var resultDesc = await Sut.Handle(new GetProductionHistoryQuery(productId, Description: "test"), default);
        resultDesc.Items.Should().HaveCount(2); // "Test A", "Test C"

        // Act & Assert 2: Amount Range
        var resultAmount = await Sut.Handle(new GetProductionHistoryQuery(productId, AmountOperator: NumericOperator.GreaterThan, Amount: 15), default);
        resultAmount.Items.Should().HaveCount(2);
        resultAmount.Items.Should().Contain(x => x.ProductionAmount == 20);
        resultAmount.Items.Should().Contain(x => x.ProductionAmount == 30);

        // Act & Assert 3: Date Range
        var resultDate = await Sut.Handle(new GetProductionHistoryQuery(productId, FromDate: baseDate.AddDays(1), ToDate: baseDate.AddDays(1).AddHours(2)), default);
        resultDate.Items.Should().HaveCount(1);
        resultDate.Items.Single().Description.Should().Be("Sample B");
    }
}
