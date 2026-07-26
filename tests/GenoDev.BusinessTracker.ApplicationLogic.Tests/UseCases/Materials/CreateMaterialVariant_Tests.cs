using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateVariant;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class CreateMaterialVariant_Tests : BusinessTrackerUnitTestsBase<CreateMaterialVariantCommandHandler>
{
    private Guid _materialId;

    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    protected override void PrepareTestData(IFixture dataGenerator)
    {
        _materialId = Arrange_BusinessTrackerDatabase(db =>
        {
            var material = new Material { Id = Guid.NewGuid(), Name = "Test Material" };
            db.Materials.Add(material);
            return material.Id;
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateMaterialVariant()
    {
        // Arrange
        var command = new CreateMaterialVariantCommand(
            MaterialId: _materialId,
            Name: "Variant 1",
            Ean: "123456789",
            ManufacturerCode: "MFG-001",
            Unit: "pcs",
            Description: "Line 1\nLine 2");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var variant = db.MaterialVariants.FirstOrDefault(x => x.Id == resultId);
            variant.Should().NotBeNull();
            variant!.MaterialId.Should().Be(_materialId);
            variant.Name.Should().Be(command.Name);
            variant.Ean.Should().Be(command.Ean);
            variant.ManufacturerCode.Should().Be(command.ManufacturerCode);
            variant.Unit.Should().Be(command.Unit);
            variant.Description.Should().Be(command.Description);
            variant.TotalUsedAmount.Should().Be(0);
            variant.TotalCompanyAmount.Should().Be(0);
            variant.TotalPrivateAmount.Should().Be(0);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenMaterialDoesNotExist()
    {
        // Arrange
        var command = new CreateMaterialVariantCommand(
            MaterialId: Guid.NewGuid(),
            Name: "Variant 1",
            Ean: null,
            ManufacturerCode: null,
            Unit: null,
            Description: null);

        // Act & Assert
        await FluentActions.Awaiting(() => Sut.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Material with ID {command.MaterialId} does not exist.");
    }
}
