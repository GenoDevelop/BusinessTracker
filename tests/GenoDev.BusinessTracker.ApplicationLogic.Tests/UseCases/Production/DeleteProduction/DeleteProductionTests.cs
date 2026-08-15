using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Production.DeleteProduction;

public class DeleteProductionTests : BusinessTrackerUnitTestsBase<DeleteProductionCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddScoped<IItemsService, ItemsService>();
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductionAndAdjustStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productionId = Guid.NewGuid();

        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Product(id: productId, name: "Product", totalAmount: 100);
            var m = db.Arrange_Material(name: "Material");
            db.Arrange_MaterialVariant(m, id: variantId, name: "Variant", companyAmount: 50, totalUsedAmount: 15);

            var production = db.Arrange_Production(id: productionId, product: db.Products.Find(productId), amount: 20);
            db.Arrange_ProductionMaterial(production: production, materialVariant: db.MaterialVariants.Find(variantId), usedAmount: 0.75);
            // 20 * 0.75 = 15
        });

        // Act
        await Sut.Handle(new DeleteProductionCommand(productionId), TestContext.Current.CancellationToken);

        // Assert
        Assert_BusinessTrackerDatabase(db =>
        {
            var deletedProduction = db.Productions.FirstOrDefault(x => x.Id == productionId);
            deletedProduction.Should().BeNull();

            var product = db.Products.First(x => x.Id == productId);
            product.TotalAmount.Should().Be(80); // 100 - 20

            var variant = db.MaterialVariants.First(x => x.Id == variantId);
            variant.TotalCompanyAmount.Should().Be(50); // Should remain unchanged
            variant.TotalUsedAmount.Should().Be(0); // 15 - 15
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowExceptionIfProductionNotFound()
    {
        // Act
        Func<Task> act = async () => await Sut.Handle(new DeleteProductionCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<GenoDev.BusinessTracker.ApplicationLogic.Exceptions.RequestValidationException>();
    }
}
