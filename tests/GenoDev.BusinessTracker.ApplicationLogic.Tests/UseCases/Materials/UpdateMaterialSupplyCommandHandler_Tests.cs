using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class UpdateMaterialSupplyCommandHandler_Tests : BusinessTrackerUnitTestsBase<UpdateMaterialSupplyCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSupplySuccessfully()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid initialSupplierId = Guid.Empty;
        Guid newSupplierId = Guid.Empty;
        
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier1 = db.Arrange_Supplier();
            var supplier2 = db.Arrange_Supplier();
            var supply = db.Arrange_MaterialSupply(supplier: supplier1);
            
            supplyId = supply.Id;
            initialSupplierId = supplier1.Id;
            newSupplierId = supplier2.Id;
        });

        var newOrderDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Unspecified);
        var newStatus = MaterialSupplyStatus.Received;
        var newDescription = "Updated Description";
        var newInvoiceNo = "INV-2026-UPDATED";

        var command = new UpdateMaterialSupplyCommand(
            supplyId,
            newSupplierId,
            newOrderDate,
            newStatus,
            newDescription,
            newInvoiceNo);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var updatedSupply = db.MaterialSupplies.First(x => x.Id == supplyId);
            updatedSupply.SupplierId.Should().Be(newSupplierId);
            updatedSupply.OrderDate.Should().Be(newOrderDate);
            updatedSupply.Status.Should().Be(newStatus);
            updatedSupply.Description.Should().Be(newDescription);
            updatedSupply.InvoiceNo.Should().Be(newInvoiceNo);
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothingIfSupplyNotFound()
    {
        // Arrange
        var command = new UpdateMaterialSupplyCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            MaterialSupplyStatus.Ordered,
            "Desc",
            "INV");

        // Act
        var act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ShouldAddMaterialAmount_WhenStatusChangedToReceived()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialMaterialAmount = 100;

        Guid supplierId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier();
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(supplier: supplier, status: MaterialSupplyStatus.Ordered);
            db.Arrange_MaterialSupplyItem(supply, material, setsAmount: setsAmount, unitsInSet: unitsInSet);

            supplyId = supply.Id;
            materialId = material.Id;
            supplierId = supplier.Id;
        });

        var command = new UpdateMaterialSupplyCommand(
            supplyId,
            supplierId,
            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            MaterialSupplyStatus.Received,
            "Delivered",
            "INV-123");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount + (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldSubtractMaterialAmount_WhenStatusDemotedFromReceived()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialMaterialAmount = 150;

        Guid supplierId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier();
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(supplier: supplier, status: MaterialSupplyStatus.Received);
            db.Arrange_MaterialSupplyItem(supply, material, setsAmount: setsAmount, unitsInSet: unitsInSet);

            supplyId = supply.Id;
            materialId = material.Id;
            supplierId = supplier.Id;
        });

        var command = new UpdateMaterialSupplyCommand(
            supplyId,
            supplierId,
            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            MaterialSupplyStatus.Ordered,
            "Demoted",
            "INV-123");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount - (setsAmount * unitsInSet));
        });
    }

    [Fact]
    public async Task Handle_ShouldNotChangeMaterialAmount_WhenStatusChangedBetweenNotReceivedStates()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Guid materialId = Guid.Empty;
        int setsAmount = 5;
        double unitsInSet = 10;
        double initialMaterialAmount = 100;

        Guid supplierId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier();
            var material = db.Arrange_Material(amount: initialMaterialAmount);
            var supply = db.Arrange_MaterialSupply(supplier: supplier, status: MaterialSupplyStatus.New);
            db.Arrange_MaterialSupplyItem(supply, material, setsAmount: setsAmount, unitsInSet: unitsInSet);

            supplyId = supply.Id;
            materialId = material.Id;
            supplierId = supplier.Id;
        });

        var command = new UpdateMaterialSupplyCommand(
            supplyId,
            supplierId,
            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            MaterialSupplyStatus.Ordered,
            "Updated",
            "INV-123");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.First(x => x.Id == materialId);
            material.Amount.Should().Be(initialMaterialAmount);
        });
    }
}
