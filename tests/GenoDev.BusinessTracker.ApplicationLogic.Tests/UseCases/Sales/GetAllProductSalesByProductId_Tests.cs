using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.GetByProduct;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Sales;

public class GetAllProductSalesByProductId_Tests : BusinessTrackerUnitTestsBase<GetAllProductSalesByProductIdQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnFilteredAndAggregatedProductSales()
    {
        // Arrange
        var product1 = new Product { ProductName = "Product 1" };
        var product2 = new Product { ProductName = "Product 2" };
        var taxRate = new TaxRate { TaxRateName = "VAT 23%", VatRate = 0.23m, TaxRateValue = 23 };
        
        var sale1 = new Sale { SaleTime = DateTimeOffset.UtcNow, SaleIdentifier = "S1" };
        var sale2 = new Sale { SaleTime = DateTimeOffset.UtcNow, SaleIdentifier = "S2" };

        var ps1 = new ProductSale
        {
            Product = product1,
            TaxRate = taxRate,
            Sale = sale1,
            Quantity = 2,
            SalePriceGross = 100,
            Description = "Desc 1"
        };
        var ps2 = new ProductSale
        {
            Product = product1,
            TaxRate = taxRate,
            Sale = sale2,
            Quantity = 3,
            SalePriceGross = 200,
            Description = "Desc 2"
        };
        var ps3 = new ProductSale
        {
            Product = product2,
            TaxRate = taxRate,
            Sale = sale1,
            Quantity = 1,
            SalePriceGross = 50
        };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.AddRange(product1, product2);
            db.TaxRates.Add(taxRate);
            db.Sales.AddRange(sale1, sale2);
            db.ProductSales.AddRange(ps1, ps2, ps3);
        });

        var query = new GetAllProductSalesByProductIdQuery(product1.Id, 0, 10);

        // Act
        var result = await Sut.Handle(query, default);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        
        var item1 = result.Items.First(i => i.Id == ps1.Id);
        item1.SaleId.Should().Be(sale1.Id);
        item1.Quantity.Should().Be(2);
        item1.SalePriceGrossEach.Should().Be(100);
        item1.SalePriceNetEach.Should().Be(100 - (100 * 0.23m));
        item1.SalePriceGrossSum.Should().Be(2 * 100);
        item1.SalePriceNetSum.Should().Be(2 * (100 - (100 * 0.23m)));
        item1.Description.Should().Be("Desc 1");
        item1.SaleIdentifier.Should().Be("S1");
        item1.TaxRateName.Should().Be("VAT 23%");
        item1.SaleTime.Should().BeCloseTo(sale1.SaleTime, TimeSpan.FromMilliseconds(100));

        var item2 = result.Items.First(i => i.Id == ps2.Id);
        item2.SaleIdentifier.Should().Be("S2");
        item2.SaleTime.Should().BeCloseTo(sale2.SaleTime, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination()
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var taxRate = new TaxRate { TaxRateName = "VAT", VatRate = 0.23m };
        var sale = new Sale { SaleTime = DateTimeOffset.UtcNow };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.Add(sale);
            for (int i = 0; i < 5; i++)
            {
                db.ProductSales.Add(new ProductSale
                {
                    Product = product,
                    TaxRate = taxRate,
                    Sale = sale,
                    Quantity = i + 1,
                    SalePriceGross = 100
                });
            }
        });

        // Act
        var result = await Sut.Handle(new GetAllProductSalesByProductIdQuery(product.Id, 1, 2), default);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.HasNextPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProductSaleSortBy.Quantity, false, 1, 5)]
    [InlineData(ProductSaleSortBy.Quantity, true, 5, 1)]
    [InlineData(ProductSaleSortBy.SalePriceGrossEach, false, 10, 50)]
    [InlineData(ProductSaleSortBy.SalePriceGrossEach, true, 50, 10)]
    [InlineData(ProductSaleSortBy.SaleTime, false, 1, 2)]
    [InlineData(ProductSaleSortBy.SaleTime, true, 2, 1)]
    public async Task Handle_ShouldApplySorting(ProductSaleSortBy sortBy, bool isDescending, decimal firstExpected, decimal lastExpected)
    {
        // Arrange
        var product = new Product { ProductName = "Product" };
        var taxRate = new TaxRate { TaxRateName = "VAT", VatRate = 0m };
        var sale1 = new Sale { SaleTime = DateTimeOffset.UtcNow.AddHours(-1) };
        var sale2 = new Sale { SaleTime = DateTimeOffset.UtcNow };

        ArrangeBusinessTracker_Database(db =>
        {
            db.Products.Add(product);
            db.TaxRates.Add(taxRate);
            db.Sales.AddRange(sale1, sale2);
            db.ProductSales.Add(new ProductSale { Product = product, TaxRate = taxRate, Sale = sale1, Quantity = 1, SalePriceGross = 10 });
            db.ProductSales.Add(new ProductSale { Product = product, TaxRate = taxRate, Sale = sale2, Quantity = 5, SalePriceGross = 50 });
        });

        var query = new GetAllProductSalesByProductIdQuery(product.Id, 0, 10, sortBy, isDescending);

        // Act
        var result = await Sut.Handle(query, default);

        // Assert
        if (sortBy == ProductSaleSortBy.Quantity)
        {
            result.Items.First().Quantity.Should().Be(firstExpected);
            result.Items.Last().Quantity.Should().Be(lastExpected);
        }
        else if (sortBy == ProductSaleSortBy.SalePriceGrossEach)
        {
            result.Items.First().SalePriceGrossEach.Should().Be(firstExpected);
            result.Items.Last().SalePriceGrossEach.Should().Be(lastExpected);
        }
        else if (sortBy == ProductSaleSortBy.SaleTime)
        {
            result.Items.First().Quantity.Should().Be(firstExpected == 1 ? 1 : 5);
            result.Items.Last().Quantity.Should().Be(lastExpected == 1 ? 1 : 5);
        }
    }
}
