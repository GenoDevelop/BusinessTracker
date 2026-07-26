using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateVariant;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class UpdateMaterialVariant_Tests : BusinessTrackerUnitTestsBase<UpdateMaterialVariantCommandHandler>
{
    private Guid _variantId;

    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    protected override void PrepareTestData(IFixture dataGenerator)
    {
        _variantId = Arrange_BusinessTrackerDatabase(db =>
        {
            var material = db.Arrange_Material(name: "Test Material");
            var variant = db.Arrange_MaterialVariant(material: material, name: "Original Name", ean: "111", manufacturerCode: "MFG1", unit: "pcs", description: "Alpha");
            return variant.Id;
        });
    }

    [Fact]
    public async Task Handle_ShouldUpdateMaterialVariant()
    {
        // Arrange
        var command = new UpdateMaterialVariantCommand(
            Id: _variantId,
            Name: "Updated Name",
            Ean: "222",
            ManufacturerCode: "MFG2",
            Unit: "box",
            Description: "Updated Description");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.FirstOrDefault(x => x.Id == _variantId);
            variant.Should().NotBeNull();
            variant!.Name.Should().Be(command.Name);
            variant.Ean.Should().Be(command.Ean);
            variant.ManufacturerCode.Should().Be(command.ManufacturerCode);
            variant.Unit.Should().Be(command.Unit);
            variant.Description.Should().Be(command.Description);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenVariantDoesNotExist()
    {
        // Arrange
        var command = new UpdateMaterialVariantCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Name",
            Ean: null,
            ManufacturerCode: null,
            Unit: null,
            Description: null);

        // Act & Assert
        await FluentActions.Awaiting(() => Sut.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Material variant with ID {command.Id} does not exist.");
    }
}
