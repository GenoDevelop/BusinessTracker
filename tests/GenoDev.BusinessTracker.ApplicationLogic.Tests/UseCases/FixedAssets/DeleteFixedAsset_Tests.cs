using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Delete;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.FixedAssets;

public sealed class DeleteFixedAsset_Tests : BusinessTrackerUnitTestsBase<DeleteFixedAssetCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteFixedAsset()
    {
        // Arrange
        var fixedAsset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = "Asset to Delete"
        };

        Arrange_BusinessTrackerDatabase(db => db.FixedAssets.Add(fixedAsset));

        var command = new DeleteFixedAssetCommand(fixedAsset.Id);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var deleted = db.FixedAssets.FirstOrDefault(x => x.Id == fixedAsset.Id);
            deleted.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var command = new DeleteFixedAssetCommand(Guid.NewGuid());

        // Act
        var act = () => Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
