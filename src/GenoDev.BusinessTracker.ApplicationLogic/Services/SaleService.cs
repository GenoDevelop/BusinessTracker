using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;
using GenoDev.BusinessTracker.Domain.Entities;

namespace GenoDev.BusinessTracker.ApplicationLogic.Services;

public class SaleService : ISaleService
{
    public void AddProductSale(Sale sale, Guid productId, Guid taxRateId, double quantity, decimal salePriceGross, string? description)
    {
        sale.ProductSales.Add(new ProductSale
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            TaxRateId = taxRateId,
            SalesId = sale.Id,
            Quantity = quantity,
            SalePriceGross = salePriceGross,
            Description = description,
            Sale = sale
        });
    }

    public void AddSalesCostsAdjustment(Sale sale, string costName, decimal adjustmentValueGross, string? description)
    {
        sale.SalesCostsAdjustments.Add(new SalesCostsAdjustment
        {
            Id = Guid.NewGuid(),
            SalesId = sale.Id,
            CostName = costName,
            AdjustmentValueGross = adjustmentValueGross,
            Description = description,
            Sale = sale
        });
    }

    public SaleDto MapToDto(Sale sale)
    {
        return new SaleDto(
            sale.Id,
            sale.SaleTime,
            sale.Description,
            sale.SaleIdentifier,
            sale.PaymentIdentifier,
            sale.ProductSales.Select(ps => new ProductSaleDto(ps.Id, ps.ProductId, ps.TaxRateId, ps.Quantity, ps.SalePriceGross, ps.Description)).ToList(),
            sale.SalesCostsAdjustments.Select(sca => new SalesCostsAdjustmentDto(sca.Id, sca.CostName, sca.AdjustmentValueGross, sca.Description)).ToList()
        );
    }
}
