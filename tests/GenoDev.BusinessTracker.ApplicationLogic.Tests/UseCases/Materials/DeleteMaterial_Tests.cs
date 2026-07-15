using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class DeleteMaterial_Tests : BusinessTrackerUnitTestsBase<DeleteMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteMaterial_WhenMaterialExists()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(id: materialId, name: "To Delete");
        });

        var command = new DeleteMaterialCommand(materialId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == materialId);
            material.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenMaterialDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteMaterialCommand(nonExistentId);

        // Act & Assert
        var act = async () => await Sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
