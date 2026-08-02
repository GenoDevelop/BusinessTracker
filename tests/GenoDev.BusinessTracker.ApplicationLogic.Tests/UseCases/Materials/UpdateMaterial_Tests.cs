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
        
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(
                id: materialId,
                name: "Old Name");
        });

        var command = new UpdateMaterialCommand(
            Id: materialId,
            Name: "New Name",
            Description: "New Description");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == materialId);
            material.Should().NotBeNull();
            material!.Name.Should().Be(command.Name);
            material.Description.Should().Be(command.Description);
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
            Description: "New Description");

        // Act & Assert
        var act = async () => await Sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
