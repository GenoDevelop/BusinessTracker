using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class DeleteSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<DeleteSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteSupplySuccessfully()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier();
            var supply = db.Arrange_Supply(supplier: supplier);
            supplyId = supply.Id;
        });

        var command = new DeleteSupplyCommand(supplyId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var deletedSupply = db.Supplies.FirstOrDefault(x => x.Id == supplyId);
            deletedSupply.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothingIfSupplyNotFound()
    {
        // Arrange
        var command = new DeleteSupplyCommand(Guid.NewGuid());

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ShouldSubtractMaterialAmount_WhenDeletingReceivedSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialVariantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            supplyId = supply.Id;
            materialVariantId = variant.Id;
        });

        var command = new DeleteSupplyCommand(supplyId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == materialVariantId);
            variant.CompanyAmount.Should().Be(initialCompanyAmount - (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractPrivateMaterialAmount_WhenDeletingReceivedPrivateSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialVariantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialPrivateAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(privateAmount: initialPrivateAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Received);
            db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: true);
            supplyId = supply.Id;
            materialVariantId = variant.Id;
        });

        var command = new DeleteSupplyCommand(supplyId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == materialVariantId);
            variant.PrivateAmount.Should().Be(initialPrivateAmount - (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldNotChangeMaterialAmount_WhenDeletingNotReceivedSupply()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialVariantId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialCompanyAmount = 100;

        Arrange_BusinessTrackerDatabase(db =>
        {
            var variant = db.Arrange_MaterialVariant(companyAmount: initialCompanyAmount);
            var supply = db.Arrange_Supply(status: MaterialSupplyStatus.Ordered);
            db.Arrange_SupplyItem(supply, variant, setsAmount: setsAmount, unitsInSet: unitsInSet, privateSupply: false);
            supplyId = supply.Id;
            materialVariantId = variant.Id;
        });

        var command = new DeleteSupplyCommand(supplyId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.First(x => x.Id == materialVariantId);
            variant.CompanyAmount.Should().Be(initialCompanyAmount);
        });
    }
}
