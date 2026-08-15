using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class GetStockAdjustments_Tests : BusinessTrackerUnitTestsBase<GetStockAdjustmentsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) =>
        RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldProjectAllColumnsAndSortByDateDescendingByDefault()
    {
        var ids = ArrangeRows();

        var result = await Sut.Handle(new GetStockAdjustmentsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(2);
        result.Items.Select(x => x.Id).Should().Equal(ids.NewerId, ids.OlderId);
        result.Items[0].Should().BeEquivalentTo(new
        {
            Id = ids.NewerId,
            ItemId = ids.PackingId,
            ItemType = StockAdjustmentItemType.PackingMaterial,
            ItemName = "B Pakunek",
            Ean = "222",
            Code = "B-CODE",
            Amount = -3d,
            IsPrivate = true,
            Date = new DateOnly(2026, 8, 15),
            Unit = "pcs",
            Description = "Nowsza korekta"
        });
    }

    [Theory]
    [InlineData(StockAdjustmentSortBy.ItemName)]
    [InlineData(StockAdjustmentSortBy.ItemType)]
    [InlineData(StockAdjustmentSortBy.Ean)]
    [InlineData(StockAdjustmentSortBy.Code)]
    [InlineData(StockAdjustmentSortBy.Amount)]
    [InlineData(StockAdjustmentSortBy.IsPrivate)]
    [InlineData(StockAdjustmentSortBy.Date)]
    [InlineData(StockAdjustmentSortBy.Description)]
    public async Task Handle_ShouldSupportEverySortColumn(StockAdjustmentSortBy sortBy)
    {
        ArrangeRows();

        var ascending = await Sut.Handle(new GetStockAdjustmentsQuery(SortBy: sortBy, IsDescending: false),
            TestContext.Current.CancellationToken);
        var descending = await Sut.Handle(new GetStockAdjustmentsQuery(SortBy: sortBy, IsDescending: true),
            TestContext.Current.CancellationToken);

        ascending.Items.Select(x => x.Id).Should().Equal(descending.Items.Select(x => x.Id).Reverse());
    }

    [Fact]
    public async Task Handle_ShouldApplyAllFiltersBeforePaging()
    {
        var ids = ArrangeRows();
        var query = new GetStockAdjustmentsQuery(
            PageIndex: 0, PageSize: 1,
            ItemNameFilter: "Pakunek",
            ItemTypeFilter: [StockAdjustmentItemType.PackingMaterial],
            EanFilter: "222", CodeFilter: "B-CODE",
            AmountFilter: -3, AmountOperator: NumericOperator.Equal,
            IsPrivateFilter: true,
            StartDate: new DateOnly(2026, 8, 15), EndDate: new DateOnly(2026, 8, 15));
        query = query with { DescriptionFilter = "Nowsza" };

        var result = await Sut.Handle(query, TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.Items.Single().Id.Should().Be(ids.NewerId);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroBasedPagesWithFilteredTotalCount()
    {
        var ids = ArrangeRows();

        var first = await Sut.Handle(new GetStockAdjustmentsQuery(PageIndex: 0, PageSize: 1), TestContext.Current.CancellationToken);
        var second = await Sut.Handle(new GetStockAdjustmentsQuery(PageIndex: 1, PageSize: 1), TestContext.Current.CancellationToken);

        first.TotalCount.Should().Be(2);
        first.HasNextPage.Should().BeTrue();
        first.Items.Single().Id.Should().Be(ids.NewerId);
        second.TotalCount.Should().Be(2);
        second.HasNextPage.Should().BeFalse();
        second.Items.Single().Id.Should().Be(ids.OlderId);
    }

    [Fact]
    public async Task Handle_ShouldUsePiecesAsProductUnit()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(name: "Produkt jednostkowy");
            db.Arrange_StockAdjustment(product: product, amount: 2);
        });

        var result = await Sut.Handle(
            new GetStockAdjustmentsQuery(ItemNameFilter: "Produkt jednostkowy"),
            TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle().Which.Unit.Should().Be("szt.");
    }

    private (Guid OlderId, Guid NewerId, Guid PackingId) ArrangeRows() => Arrange_BusinessTrackerDatabase(db =>
    {
        var variant = db.Arrange_MaterialVariant(name: "A Materiał", ean: "111", manufacturerCode: "A-CODE");
        var packing = db.Arrange_PackingMaterial(name: "B Pakunek", ean: "222", manufacturerCode: "B-CODE");
        var older = db.Arrange_StockAdjustment(materialVariant: variant, amount: 2,
            date: new DateOnly(2026, 8, 10), description: "Starsza korekta");
        var newer = db.Arrange_StockAdjustment(packingMaterial: packing, amount: -3, isPrivate: true,
            date: new DateOnly(2026, 8, 15), description: "Nowsza korekta");
        return (older.Id, newer.Id, packing.Id);
    });
}
