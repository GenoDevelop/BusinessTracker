using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.FixedAssets;

public sealed class CreateFixedAsset_Tests : BusinessTrackerUnitTestsBase<CreateFixedAssetCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateFixedAsset()
    {
        // Arrange
        var command = new CreateFixedAssetCommand(
            "Test Fixed Asset",
            "123456789",
            "MC001",
            "pcs",
            "Test Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        
        Assert_BusinessTrackerDatabase(db =>
        {
            var created = db.FixedAssets.FirstOrDefault(x => x.Id == result);
            created.Should().NotBeNull();
            created!.Name.Should().Be(command.Name);
            created.Ean.Should().Be(command.Ean);
            created.ManufacturerCode.Should().Be(command.ManufacturerCode);
            created.Unit.Should().Be(command.Unit);
            created.Description.Should().Be(command.Description);
        });
    }
}
