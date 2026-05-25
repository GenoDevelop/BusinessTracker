using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.Domain.Entities;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Services;

public class SaleService_Tests
{
    private readonly SaleService _sut;

    public SaleService_Tests()
    {
        _sut = new SaleService();
    }

    [Fact]
    public void AddProductSale_ShouldAddProductSaleToSale()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        var productId = Guid.NewGuid();
        var taxRateId = Guid.NewGuid();
        var quantity = 5.5;
        var salePriceGross = 100.50m;
        var description = "Test product sale description";

        // Act
        _sut.AddProductSale(sale, productId, taxRateId, quantity, salePriceGross, description);

        // Assert
        sale.ProductSales.Should().HaveCount(1);
        var productSale = sale.ProductSales.First();
        productSale.Id.Should().NotBeEmpty();
        productSale.ProductId.Should().Be(productId);
        productSale.TaxRateId.Should().Be(taxRateId);
        productSale.SalesId.Should().Be(sale.Id);
        productSale.Quantity.Should().Be(quantity);
        productSale.SalePriceGross.Should().Be(salePriceGross);
        productSale.Decription.Should().Be(description);
        productSale.Sale.Should().Be(sale);
    }

    [Fact]
    public void AddSalesCostsAdjustment_ShouldAddAdjustmentToSale()
    {
        // Arrange
        var sale = new Sale { Id = Guid.NewGuid() };
        var costName = "Shipping";
        var adjustmentValueGross = 15.00m;
        var description = "Shipping cost adjustment";

        // Act
        _sut.AddSalesCostsAdjustment(sale, costName, adjustmentValueGross, description);

        // Assert
        sale.SalesCostsAdjustments.Should().HaveCount(1);
        var adjustment = sale.SalesCostsAdjustments.First();
        adjustment.Id.Should().NotBeEmpty();
        adjustment.SalesId.Should().Be(sale.Id);
        adjustment.CostName.Should().Be(costName);
        adjustment.AdjustmentValueGross.Should().Be(adjustmentValueGross);
        adjustment.Description.Should().Be(description);
        adjustment.Sale.Should().Be(sale);
    }
}
