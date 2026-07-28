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
    public async Task Handle_ShouldCreateMaterial()
    {
        // Arrange
        var command = new CreateMaterialCommand(
            Name: "Full Material",
            Description: "Material Description");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var material = db.Materials.FirstOrDefault(x => x.Id == resultId);
            material.Should().NotBeNull();
            material!.Name.Should().Be(command.Name);
            material.Description.Should().Be(command.Description);
        });
    }

}
