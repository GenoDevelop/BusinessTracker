using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class UpdateStockAdjustment_Tests : BusinessTrackerUnitTestsBase<UpdateStockAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) =>
        RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldRevertPreviousEffectAndApplyNewEffect()
    {
        var ids = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(companyAmount: 15);
            var product = db.Arrange_Product(totalAmount: 10);
            var adjustment = db.Arrange_StockAdjustment(materialVariant: variant, amount: 5);
            return (AdjustmentId: adjustment.Id, VariantId: variant.Id, ProductId: product.Id);
        });
        var date = new DateOnly(2026, 8, 10);

        await Sut.Handle(new UpdateStockAdjustmentCommand(ids.AdjustmentId, date,
            StockAdjustmentItemType.Product, ids.ProductId, -3, false, "Korekta po przeliczeniu"), TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            db.MaterialVariants.Single(x => x.Id == ids.VariantId).TotalCompanyAmount.Should().Be(10);
            db.Products.Single(x => x.Id == ids.ProductId).TotalAmount.Should().Be(7);
            var adjustment = db.StockAdjustments.Single(x => x.Id == ids.AdjustmentId);
            adjustment.ItemType.Should().Be(StockAdjustmentItemType.Product);
            adjustment.ProductId.Should().Be(ids.ProductId);
            adjustment.MaterialVariantId.Should().BeNull();
            adjustment.Amount.Should().Be(-3);
            adjustment.Date.Should().Be(date);
            adjustment.Description.Should().Be("Korekta po przeliczeniu");
        });
    }
}
