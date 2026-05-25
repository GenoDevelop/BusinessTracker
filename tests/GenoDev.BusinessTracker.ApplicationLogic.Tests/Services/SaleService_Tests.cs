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
        productSale.Description.Should().Be(description);
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

    [Fact]
    public void MapToDto_ShouldMapSaleToDtoCorrectly()
    {
        // Arrange
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleTime = DateTimeOffset.UtcNow,
            Description = "Main description",
            SaleIdentifier = "SALE-001",
            PaymentIdentifier = "PAY-001"
        };

        var psId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxRateId = Guid.NewGuid();
        sale.ProductSales.Add(new ProductSale
        {
            Id = psId,
            ProductId = productId,
            TaxRateId = taxRateId,
            Quantity = 2,
            SalePriceGross = 50m,
            Description = "PS Desc",
            SalesId = sale.Id,
            Sale = sale
        });

        var scaId = Guid.NewGuid();
        sale.SalesCostsAdjustments.Add(new SalesCostsAdjustment
        {
            Id = scaId,
            CostName = "Extra",
            AdjustmentValueGross = 10m,
            Description = "SCA Desc",
            SalesId = sale.Id,
            Sale = sale
        });

        // Act
        var result = _sut.MapToDto(sale);

        // Assert
        result.Id.Should().Be(sale.Id);
        result.SaleTime.Should().Be(sale.SaleTime);
        result.Description.Should().Be(sale.Description);
        result.SaleIdentifier.Should().Be(sale.SaleIdentifier);
        result.PaymentIdentifier.Should().Be(sale.PaymentIdentifier);

        result.ProductSales.Should().HaveCount(1);
        result.ProductSales[0].Id.Should().Be(psId);
        result.ProductSales[0].ProductId.Should().Be(productId);
        result.ProductSales[0].TaxRateId.Should().Be(taxRateId);
        result.ProductSales[0].Quantity.Should().Be(2);
        result.ProductSales[0].SalePriceGross.Should().Be(50m);
        result.ProductSales[0].Description.Should().Be("PS Desc");

        result.SalesCostsAdjustments.Should().HaveCount(1);
        result.SalesCostsAdjustments[0].Id.Should().Be(scaId);
        result.SalesCostsAdjustments[0].CostName.Should().Be("Extra");
        result.SalesCostsAdjustments[0].AdjustmentValueGross.Should().Be(10m);
        result.SalesCostsAdjustments[0].Description.Should().Be("SCA Desc");
    }
}
