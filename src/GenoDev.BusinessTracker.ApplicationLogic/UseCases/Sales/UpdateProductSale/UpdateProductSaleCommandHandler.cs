using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateProductSale;

public class UpdateProductSaleCommandHandler : IRequestHandler<UpdateProductSaleCommand, ProductSaleDto>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateProductSaleCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<ProductSaleDto> Handle(UpdateProductSaleCommand request, CancellationToken cancellationToken)
    {
        var productSale = await _context.ProductSales
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        productSale.TaxRateId = request.TaxRateId;
        productSale.Quantity = request.Quantity;
        productSale.SalePriceGross = request.SalePriceGross;
        productSale.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductSaleDto(
            productSale.Id,
            productSale.ProductId,
            productSale.TaxRateId,
            productSale.Quantity,
            productSale.SalePriceGross,
            productSale.Description);
    }
}
