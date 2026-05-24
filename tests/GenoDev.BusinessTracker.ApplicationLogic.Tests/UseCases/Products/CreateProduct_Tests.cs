using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public class CreateProduct_Tests : BusinessTrackerUnitTestsBase<CreateProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenValidInputProvided()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product", "1234567890123");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductName.Should().Be(command.ProductName);
        result.EanCode.Should().Be(command.EanCode);
        result.Id.Should().NotBeEmpty();

        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == result.Id);
            product.Should().NotBeNull();
            product!.ProductName.Should().Be(command.ProductName);
            product.EanCode.Should().Be(command.EanCode);
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenEanCodeIsNull()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product No EAN", null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductName.Should().Be(command.ProductName);
        result.EanCode.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == result.Id);
            product.Should().NotBeNull();
            product!.ProductName.Should().Be(command.ProductName);
            product.EanCode.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateTwoProducts_WhenBothHaveNullEanCode()
    {
        // Arrange
        var command1 = new CreateProductCommand("Product 1", null);
        var command2 = new CreateProductCommand("Product 2", null);

        // Act
        var result1 = await Sut.Handle(command1, CancellationToken.None);
        var result2 = await Sut.Handle(command2, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Id.Should().NotBe(result2.Id);

        AssertBusinessTracker_Database(db =>
        {
            var product1 = db.Products.FirstOrDefault(x => x.Id == result1.Id);
            var product2 = db.Products.FirstOrDefault(x => x.Id == result2.Id);
            
            product1.Should().NotBeNull();
            product1!.EanCode.Should().BeNull();
            
            product2.Should().NotBeNull();
            product2!.EanCode.Should().BeNull();
        });
    }
}
