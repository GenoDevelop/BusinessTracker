using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.TaxRates;

public class GetAllTaxRates_Tests : BusinessTrackerUnitTestsBase<GetAllTaxRatesQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Theory]
    [InlineData(TaxRateSortBy.Name, false, "A", "B", "C")]
    [InlineData(TaxRateSortBy.Name, true, "C", "B", "A")]
    [InlineData(TaxRateSortBy.VatRate, false, "C", "A", "B")]
    [InlineData(TaxRateSortBy.VatRate, true, "B", "A", "C")]
    [InlineData(TaxRateSortBy.TaxRate, false, "C", "A", "B")]
    [InlineData(TaxRateSortBy.TaxRate, true, "B", "A", "C")]
    public async Task Handle_ShouldReturnSortedTaxRates(TaxRateSortBy sortBy, bool isDescending, string first, string second, string third)
    {
        // Arrange
        var rate1 = new TaxRate { TaxRateName = "A", VatRate = 0.1m, TaxRateValue = 0.1m };
        var rate2 = new TaxRate { TaxRateName = "B", VatRate = 0.2m, TaxRateValue = 0.2m };
        var rate3 = new TaxRate { TaxRateName = "C", VatRate = 0.05m, TaxRateValue = 0.05m };
        ArrangeBusinessTracker_Database(db => db.TaxRates.AddRange(rate1, rate2, rate3));

        var query = new GetAllTaxRatesQuery(0, 10, sortBy, isDescending);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].TaxRateName.Should().Be(first);
        result.Items[1].TaxRateName.Should().Be(second);
        result.Items[2].TaxRateName.Should().Be(third);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedTaxRates()
    {
        // Arrange
        var rate1 = new TaxRate { TaxRateName = "A", VatRate = 0.1m, TaxRateValue = 0.1m };
        var rate2 = new TaxRate { TaxRateName = "B", VatRate = 0.2m, TaxRateValue = 0.2m };
        var rate3 = new TaxRate { TaxRateName = "C", VatRate = 0.05m, TaxRateValue = 0.05m };
        ArrangeBusinessTracker_Database(db => db.TaxRates.AddRange(rate1, rate2, rate3));

        var query = new GetAllTaxRatesQuery(0, 2, TaxRateSortBy.Name, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }
}