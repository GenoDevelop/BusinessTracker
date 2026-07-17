using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production;

public class CreateRecipe_Tests : BusinessTrackerUnitTestsBase<CreateRecipeCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateRecipeWithAllData()
    {
        // Arrange
        Guid productId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var product = db.Arrange_Product(name: "Test Product");
            productId = product.Id;
        });

        var command = new CreateRecipeCommand(
            ProductId: productId,
            Name: "Test Recipe",
            Description: "Recipe Description");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var recipe = db.ProductRecipes.FirstOrDefault(x => x.Id == resultId);
            recipe.Should().NotBeNull();
            recipe!.Name.Should().Be(command.Name);
            recipe.Description.Should().Be(command.Description);
            recipe.ProductId.Should().Be(command.ProductId);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenProductDoesNotExist()
    {
        // Arrange
        var command = new CreateRecipeCommand(
            ProductId: Guid.NewGuid(),
            Name: "Invalid Recipe",
            Description: null);

        // Act
        Func<Task> act = async () => await Sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
