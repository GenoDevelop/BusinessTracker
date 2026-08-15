using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class CreateStockAdjustments_Tests : BusinessTrackerUnitTestsBase<CreateStockAdjustmentsCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) =>
        RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldPersistSeveralCategoriesAndAdjustCorrectAmounts()
    {
        var ids = Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(companyAmount: 10);
            var packing = db.Arrange_PackingMaterial(totalPrivateAmount: 20);
            var asset = db.Arrange_FixedAsset(totalCompanyAmount: 5);
            var product = db.Arrange_Product(totalAmount: 12);
            return (VariantId: variant.Id, PackingId: packing.Id, AssetId: asset.Id, ProductId: product.Id);
        });
        var date = new DateOnly(2026, 8, 15);
        var command = new CreateStockAdjustmentsCommand(date,
        [
            new(StockAdjustmentItemType.MaterialVariant, ids.VariantId, 2.5, false),
            new(StockAdjustmentItemType.PackingMaterial, ids.PackingId, -3, true),
            new(StockAdjustmentItemType.FixedAsset, ids.AssetId, 4, false),
            new(StockAdjustmentItemType.Product, ids.ProductId, -2, false)
        ], "Inwentaryzacja roczna");

        var createdIds = await Sut.Handle(command, TestContext.Current.CancellationToken);

        createdIds.Should().HaveCount(4);
        Assert_BusinessTrackerDatabase(db =>
        {
            db.StockAdjustments.Where(x => createdIds.Contains(x.Id)).Should().HaveCount(4);
            db.MaterialVariants.Single(x => x.Id == ids.VariantId).TotalCompanyAmount.Should().Be(12.5);
            db.PackingMaterials.Single(x => x.Id == ids.PackingId).TotalPrivateAmount.Should().Be(17);
            db.FixedAssets.Single(x => x.Id == ids.AssetId).TotalCompanyAmount.Should().Be(9);
            db.Products.Single(x => x.Id == ids.ProductId).TotalAmount.Should().Be(10);
            db.StockAdjustments.Select(x => x.Date).Should().OnlyContain(x => x == date);
            db.StockAdjustments.Select(x => x.Description).Should().OnlyContain(x => x == "Inwentaryzacja roczna");
        });
    }

    [Fact]
    public async Task Handle_WhenItemWasRemoved_ShouldThrowValidationException()
    {
        var command = new CreateStockAdjustmentsCommand(DateOnly.FromDateTime(DateTime.Today),
            [new(StockAdjustmentItemType.Product, Guid.NewGuid(), 1, false)]);

        await Sut.Invoking(x => x.Handle(command, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
