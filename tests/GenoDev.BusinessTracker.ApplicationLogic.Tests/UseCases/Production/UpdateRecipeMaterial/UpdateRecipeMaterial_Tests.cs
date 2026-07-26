using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.UpdateRecipeMaterial;

public class UpdateRecipeMaterial_Tests : BusinessTrackerUnitTestsBase<UpdateRecipeMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMaterialId()
    {
        // Arrange
        var recipeMaterialId = Guid.NewGuid();
        var oldMaterialId = Guid.NewGuid();
        var newMaterialId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            var oldMaterial = db.Arrange_Material(id: oldMaterialId);
            var newMaterial = db.Arrange_Material(id: newMaterialId);
            db.Arrange_ProductRecipeMaterial(id: recipeMaterialId, material: oldMaterial);
        });

        var command = new UpdateRecipeMaterialCommand(recipeMaterialId, newMaterialId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var updated = db.ProductRecipeMaterials.Find(recipeMaterialId);
            updated.Should().NotBeNull();
            updated!.MaterialId.Should().Be(newMaterialId);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var command = new UpdateRecipeMaterialCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
