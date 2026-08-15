using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.PackingMaterials;

public sealed class DeletePackingMaterial_Tests : BusinessTrackerUnitTestsBase<DeletePackingMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeletePackingMaterial()
    {
        // Arrange
        var packingMaterial = new PackingMaterial
        {
            Id = Guid.NewGuid(),
            Name = "To Be Deleted"
        };

        Arrange_BusinessTrackerDatabase(db => db.PackingMaterials.Add(packingMaterial));

        var command = new DeletePackingMaterialCommand(packingMaterial.Id);

        // Act
        await Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var deleted = db.PackingMaterials.FirstOrDefault(x => x.Id == packingMaterial.Id);
            deleted.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var command = new DeletePackingMaterialCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
