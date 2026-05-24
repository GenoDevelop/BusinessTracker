using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Products;

public class GetAllProducts_Tests : BusinessTrackerUnitTestsBase<GetAllProductsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedProducts()
    {
        // Arrange
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.AddRange(Enumerable.Range(1, 10).Select(i => new Product
            {
                ProductName = $"Product {i:D2}",
                EanCode = $"{i:D13}"
            }));
        });

        var query = new GetAllProductsQuery(1, 3, ProductSortBy.Name, false); // Page 1, Size 3 (0-based, so items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].ProductName.Should().Be("Product 04");
        result.Items[1].ProductName.Should().Be("Product 05");
        result.Items[2].ProductName.Should().Be("Product 06");
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending()
    {
        // Arrange
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(new Product { ProductName = "A", EanCode = "1" });
            db.Products.Add(new Product { ProductName = "C", EanCode = "3" });
            db.Products.Add(new Product { ProductName = "B", EanCode = "2" });
        });

        var query = new GetAllProductsQuery(0, 10, ProductSortBy.Name, true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.ProductName).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public async Task Handle_ShouldSortByEanAscending()
    {
        // Arrange
        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(new Product { ProductName = "Prod 1", EanCode = "333" });
            db.Products.Add(new Product { ProductName = "Prod 2", EanCode = "111" });
            db.Products.Add(new Product { ProductName = "Prod 3", EanCode = "222" });
        });

        var query = new GetAllProductsQuery(0, 10, ProductSortBy.Ean, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.EanCode).Should().ContainInOrder("111", "222", "333");
    }
}
