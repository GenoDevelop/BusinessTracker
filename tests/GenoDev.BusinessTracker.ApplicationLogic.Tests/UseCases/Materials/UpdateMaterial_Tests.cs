using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Update;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class UpdateMaterial_Tests : BusinessTrackerUnitTestsBase<UpdateMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMaterialData()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        const double originalAmount = 50.0;
        
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(
                id: materialId,
                name: "Old Name",
                ean: "Old Ean",
                description: "Old Description",
                unit: "pcs",
                amount: originalAmount);
        });

        var command = new UpdateMaterialCommand(
            Id: materialId,
            Name: "New Name",
            Ean: "New Ean",
            Description: "New Description",
            Unit: "kg");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == materialId);
            material.Should().NotBeNull();
            material!.Name.Should().Be(command.Name);
            material.Ean.Should().Be(command.Ean);
            material.Description.Should().Be(command.Description);
            material.Unit.Should().Be(command.Unit);
            material.Amount.Should().Be(originalAmount); // Amount should remain unchanged
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenMaterialDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateMaterialCommand(
            Id: nonExistentId,
            Name: "New Name",
            Ean: "New Ean",
            Description: "New Description",
            Unit: "kg");

        // Act & Assert
        var act = async () => await Sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
