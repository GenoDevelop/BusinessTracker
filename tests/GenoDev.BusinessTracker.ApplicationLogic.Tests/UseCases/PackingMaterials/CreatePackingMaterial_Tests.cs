using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.PackingMaterials;

public sealed class CreatePackingMaterial_Tests : BusinessTrackerUnitTestsBase<CreatePackingMaterialCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreatePackingMaterial()
    {
        // Arrange
        var command = new CreatePackingMaterialCommand(
            "Test Packing Material",
            "123456789",
            "MC001",
            "pcs",
            "Test Description");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        
        AssertBusinessTracker_Database(db =>
        {
            var created = db.PackingMaterials.FirstOrDefault(x => x.Id == result);
            created.Should().NotBeNull();
            created!.Name.Should().Be(command.Name);
            created.Ean.Should().Be(command.Ean);
            created.ManufacturerCode.Should().Be(command.ManufacturerCode);
            created.Unit.Should().Be(command.Unit);
            created.Description.Should().Be(command.Description);
        });
    }

    [Fact]
    public async Task Handle_ShouldNullifyEmptyStrings()
    {
        // Arrange
        var command = new CreatePackingMaterialCommand(
            "Test Packing Material",
            " ",
            "",
            "pcs",
            null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var created = db.PackingMaterials.FirstOrDefault(x => x.Id == result);
            created.Should().NotBeNull();
            created!.Ean.Should().BeNull();
            created.ManufacturerCode.Should().BeNull();
        });
    }
}
