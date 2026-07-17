using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class CreateProduct_Tests : BusinessTrackerUnitTestsBase<CreateProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductWithAllData()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Full Product",
            Identifier: "PROD-001",
            Description: "Detailed product description");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == resultId);
            product.Should().NotBeNull();
            product!.Name.Should().Be(command.Name);
            product.Identifier.Should().Be(command.Identifier);
            product.Description.Should().Be(command.Description);
            product.Amount.Should().Be(0); // Initial amount must be 0
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateProductWithMinimalData()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Minimal Product",
            Identifier: "PROD-MIN",
            Description: null);

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == resultId);
            product.Should().NotBeNull();
            product!.Name.Should().Be(command.Name);
            product.Identifier.Should().Be(command.Identifier);
            product.Description.Should().BeNull();
            product.Amount.Should().Be(0);
        });
    }
}
