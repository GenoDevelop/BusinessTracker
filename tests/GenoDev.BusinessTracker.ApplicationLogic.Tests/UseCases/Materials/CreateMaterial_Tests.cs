using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class CreateMaterial_Tests : BusinessTrackerUnitTestsBase<CreateMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateMaterialWithAllData()
    {
        // Arrange
        var command = new CreateMaterialCommand(
            Name: "Full Material",
            Ean: "1234567890",
            Description: "Test Description",
            Unit: "kg");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == resultId);
            material.Should().NotBeNull();
            material!.Name.Should().Be(command.Name);
            material.Ean.Should().Be(command.Ean);
            material.Description.Should().Be(command.Description);
            material.Unit.Should().Be(command.Unit);
            material.Amount.Should().Be(0); // Explicitly required to be 0
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateMaterialWithMinimalData()
    {
        // Arrange
        var command = new CreateMaterialCommand(
            Name: "Minimal Material",
            Ean: null,
            Description: null,
            Unit: null);

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == resultId);
            material.Should().NotBeNull();
            material!.Name.Should().Be(command.Name);
            material.Ean.Should().BeNull();
            material.Description.Should().BeNull();
            material.Unit.Should().BeNull();
            material.Amount.Should().Be(0);
        });
    }
}
