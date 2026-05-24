using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public class UpdateProduct_Tests : BusinessTrackerUnitTestsBase<UpdateProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProduct_WhenValidInputProvided()
    {
        // Arrange
        var product = new Product
        {
            ProductName = "Old Name",
            EanCode = "123"
        };
        ArrangeBusinessTracker_Database(db => db.Products.Add(product));

        var command = new UpdateProductCommand(product.Id, "New Name", "456");

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProductName.Should().Be(command.ProductName);
        result.EanCode.Should().Be(command.EanCode);
        result.Id.Should().Be(product.Id);

        AssertBusinessTracker_Database(db =>
        {
            var updatedProduct = db.Products.FirstOrDefault(x => x.Id == product.Id);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.ProductName.Should().Be(command.ProductName);
            updatedProduct.EanCode.Should().Be(command.EanCode);
        });
    }

    [Fact]
    public async Task Handle_ShouldUpdateProduct_WhenEanCodeIsSetToNull()
    {
        // Arrange
        var product = new Product
        {
            ProductName = "Some Product",
            EanCode = "12345"
        };
        ArrangeBusinessTracker_Database(db => db.Products.Add(product));

        var command = new UpdateProductCommand(product.Id, "Some Product", null);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.EanCode.Should().BeNull();

        AssertBusinessTracker_Database(db =>
        {
            var updatedProduct = db.Products.FirstOrDefault(x => x.Id == product.Id);
            updatedProduct!.EanCode.Should().BeNull();
        });
    }

}
