using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class DeleteStockAdjustment_Tests : BusinessTrackerUnitTestsBase<DeleteStockAdjustmentCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) =>
        RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldDeleteAdjustmentAndRevertItsPrivateAmount()
    {
        var ids = Arrange_BusinessTrackerDatabase(db =>
        {
            var packing = db.Arrange_PackingMaterial(totalPrivateAmount: 8);
            var adjustment = db.Arrange_StockAdjustment(packingMaterial: packing, amount: 3, isPrivate: true);
            return (AdjustmentId: adjustment.Id, PackingId: packing.Id);
        });

        await Sut.Handle(new DeleteStockAdjustmentCommand(ids.AdjustmentId), TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            db.StockAdjustments.Any(x => x.Id == ids.AdjustmentId).Should().BeFalse();
            db.PackingMaterials.Single(x => x.Id == ids.PackingId).TotalPrivateAmount.Should().Be(5);
        });
    }
}
