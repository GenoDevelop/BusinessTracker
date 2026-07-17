using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Delete;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class DeleteProduct_Tests : BusinessTrackerUnitTestsBase<DeleteProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product to Delete");
        });

        var command = new DeleteProductCommand(productId);

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == productId);
            product.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenProductDoesNotExist()
    {
        // Arrange
        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var act = async () => await Sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
