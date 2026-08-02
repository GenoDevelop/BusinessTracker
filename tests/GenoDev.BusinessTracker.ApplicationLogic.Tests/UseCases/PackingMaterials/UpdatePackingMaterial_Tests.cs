using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.PackingMaterials;

public sealed class UpdatePackingMaterial_Tests : BusinessTrackerUnitTestsBase<UpdatePackingMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePackingMaterial()
    {
        // Arrange
        var packingMaterial = new PackingMaterial
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Ean = "Original Ean",
            ManufacturerCode = "Original MC",
            Unit = "Original Unit",
            Description = "Original Description"
        };

        Arrange_BusinessTrackerDatabase(db => db.PackingMaterials.Add(packingMaterial));

        var command = new UpdatePackingMaterialCommand(
            packingMaterial.Id,
            "Updated Name",
            "Updated Ean",
            "Updated MC",
            "Updated Unit",
            "Updated Description");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var updated = db.PackingMaterials.FirstOrDefault(x => x.Id == packingMaterial.Id);
            updated.Should().NotBeNull();
            updated!.Name.Should().Be(command.Name);
            updated.Ean.Should().Be(command.Ean);
            updated.ManufacturerCode.Should().Be(command.ManufacturerCode);
            updated.Unit.Should().Be(command.Unit);
            updated.Description.Should().Be(command.Description);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var command = new UpdatePackingMaterialCommand(
            Guid.NewGuid(),
            "Name",
            null,
            null,
            null,
            null);

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
