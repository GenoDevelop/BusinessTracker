using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class RemoveItemFromSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<RemoveItemFromSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldRemoveItemFromSupply()
    {
        // Arrange
        var itemId = Arrange_BusinessTrackerDatabase(db =>
        {
            var supply = db.Arrange_Supply();
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material);
            var item = db.Arrange_SupplyItem(supply, variant);
            return item.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractCompanyMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.CompanyAmount.Should().Be(initialCompanyAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractPrivateMaterialAmount_WhenDeletingFromReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialPrivateAmount = 50;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, privateAmount: initialPrivateAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: true);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.PrivateAmount.Should().Be(initialPrivateAmount - (setsAmount * unitsInSet));
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_ShouldNotChangeMaterialAmount_WhenDeletingFromNotReceivedSupply()
    {
        // Arrange
        Guid itemId = Guid.Empty;
        Guid variantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material();
            var variant = db.Arrange_MaterialVariant(material, companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Ordered);
            var item = db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet);
            itemId = item.Id;
            variantId = variant.Id;
        });

        var command = new RemoveItemFromSupplyCommand(itemId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.CompanyAmount.Should().Be(initialCompanyAmount);
            db.SupplyItems.Any(x => x.Id == itemId).Should().BeFalse();
        });
    }

    [Fact]
    public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new RemoveItemFromSupplyCommand(Guid.NewGuid());

        // Act & Assert
        await Sut.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
