using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class UpdateProduct_Tests : BusinessTrackerUnitTestsBase<UpdateProductCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProductData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(
                id: productId,
                name: "Original Name",
                identifier: "ORIG-ID",
                description: "Original Description",
                amount: 10);
        });

        var command = new UpdateProductCommand(
            Id: productId,
            Name: "Updated Name",
            Identifier: "UPD-ID",
            Description: "Updated Description");

        // Act
        await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var product = db.Products.FirstOrDefault(x => x.Id == productId);
            product.Should().NotBeNull();
            product!.Name.Should().Be(command.Name);
            product.Identifier.Should().Be(command.Identifier);
            product.Description.Should().Be(command.Description);
            product.Amount.Should().Be(10); // Amount should remain unchanged
        });
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenProductDoesNotExist()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Id: Guid.NewGuid(),
            Name: "New Name",
            Identifier: "NEW-ID",
            Description: "New Description");

        // Act & Assert
        var act = async () => await Sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
