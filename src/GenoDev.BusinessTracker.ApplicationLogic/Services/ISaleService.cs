using GenoDev.BusinessTracker.Domain.Entities;

namespace GenoDev.BusinessTracker.ApplicationLogic.Services;

public interface ISaleService
{
    void AddProductSale(Sale sale, Guid productId, Guid taxRateId, double quantity, decimal salePriceGross, string? description);
    void AddSalesCostsAdjustment(Sale sale, string costName, decimal adjustmentValueGross, string? description);
}
