using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetOptions;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class GetStockAdjustmentOptions_Tests : BusinessTrackerUnitTestsBase<GetStockAdjustmentOptionsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) =>
        RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldReturnOptionsForEverySupportedCategory()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_MaterialVariant(name: "Wariant", ean: "1", manufacturerCode: "MV");
            db.Arrange_PackingMaterial(name: "Pakunek", ean: "2", manufacturerCode: "PM");
            db.Arrange_FixedAsset(name: "Środek", ean: "3", manufacturerCode: "FA");
            db.Arrange_Product(name: "Produkt", identifier: "PROD");
        });

        var result = await Sut.Handle(new GetStockAdjustmentOptionsQuery(), TestContext.Current.CancellationToken);

        result.Select(x => x.ItemType).Should().BeEquivalentTo(Enum.GetValues<StockAdjustmentItemType>());
        result.Single(x => x.ItemType == StockAdjustmentItemType.Product).Code.Should().Be("PROD");
        result.Single(x => x.ItemType == StockAdjustmentItemType.Product).Unit.Should().Be("szt.");
        result.Single(x => x.ItemType == StockAdjustmentItemType.MaterialVariant).DisplayName.Should().Contain("Wariant");
    }
}
