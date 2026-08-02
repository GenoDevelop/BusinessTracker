using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteVariant;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class DeleteMaterialVariant_Tests : BusinessTrackerUnitTestsBase<DeleteMaterialVariantCommandHandler>
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
            var variant = db.Arrange_MaterialVariant(material: material, name: "To Delete");
            return variant.Id;
        });
    }

    [Fact]
    public async Task Handle_ShouldDeleteMaterialVariant()
    {
        // Arrange
        var command = new DeleteMaterialVariantCommand(_variantId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var variant = db.MaterialVariants.FirstOrDefault(x => x.Id == _variantId);
            variant.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenVariantDoesNotExist()
    {
        // Arrange
        var command = new DeleteMaterialVariantCommand(Guid.NewGuid());

        // Act & Assert
        await FluentActions.Awaiting(() => Sut.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Material variant with ID {command.Id} does not exist.");
    }
}
