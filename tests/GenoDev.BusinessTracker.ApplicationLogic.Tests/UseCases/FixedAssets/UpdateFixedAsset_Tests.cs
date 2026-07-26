using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.FixedAssets;

public sealed class UpdateFixedAsset_Tests : BusinessTrackerUnitTestsBase<UpdateFixedAssetCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateFixedAsset()
    {
        // Arrange
        var fixedAsset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Ean = "Original Ean",
            ManufacturerCode = "Original MC",
            Unit = "Original Unit",
            Description = "Original Description"
        };

        Arrange_BusinessTrackerDatabase(db => db.FixedAssets.Add(fixedAsset));

        var command = new UpdateFixedAssetCommand(
            fixedAsset.Id,
            "Updated Name",
            "Updated Ean",
            "Updated MC",
            "Updated Unit",
            "Updated Description");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var updated = db.FixedAssets.FirstOrDefault(x => x.Id == fixedAsset.Id);
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
        var command = new UpdateFixedAssetCommand(
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
