using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddSupplyItem;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class AddMaterialToSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<AddMaterialToSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldAddMaterialToSupply()
    {
        // Arrange
        var (supplyId, materialId) = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            var material = db.Arrange_Material();
            return (supply.Id, material.Id);
        });

        var command = new AddMaterialToSupplyCommand(
            supplyId,
            materialId,
            SetsAmount: 5,
            UnitsInSet: 10,
            SetNetPrice: 100,
            SetGrossPrice: 123);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var item = db.MaterialSupplyItems
                .FirstOrDefault(x => x.MaterialSupplyId == supplyId && x.MaterialId == materialId);

            item.Should().NotBeNull();
            item!.SetsAmount.Should().Be(5);
            item.UnitsInSet.Should().Be(10);
            item.SetNetPrice.Should().Be(100);
            item.SetGrossPrice.Should().Be(123);
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentSupply_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var materialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            return material.Id;
        });

        var command = new AddMaterialToSupplyCommand(
            Guid.NewGuid(),
            materialId,
            5, 10, 100, 123);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentMaterial_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var supplyId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_MaterialSupply();
            return supply.Id;
        });

        var command = new AddMaterialToSupplyCommand(
            supplyId,
            Guid.NewGuid(),
            5, 10, 100, 123);

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
