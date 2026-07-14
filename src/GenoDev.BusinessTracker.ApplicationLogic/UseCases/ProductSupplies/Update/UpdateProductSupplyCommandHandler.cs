using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Update;

public class UpdateProductSupplyCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<UpdateProductSupplyCommand, ProductSupplyDto>
{
    public async Task<ProductSupplyDto> Handle(UpdateProductSupplyCommand request, CancellationToken cancellationToken)
    {
        var productSupply = await dbContext.ProductSupplies
            .FirstAsync(x => x.Id == request.Id, cancellationToken);

        productSupply.SupplierId = request.SupplierId;
        productSupply.BuyPriceNet = request.BuyPriceNet;
        productSupply.BuyPriceGross = request.BuyPriceGross;
        productSupply.Quantity = request.Quantity;
        productSupply.SupplyStatus = request.SupplyStatus;
        productSupply.BuyTime = request.BuyTime?.ToUniversalTime();
        productSupply.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductSupplyDto
        {
            Id = productSupply.Id,
            ProductId = productSupply.ProductId,
            BuyPriceNet = productSupply.BuyPriceNet,
            BuyPriceGross = productSupply.BuyPriceGross,
            BuyTime = productSupply.BuyTime,
            Quantity = productSupply.Quantity,
            Description = productSupply.Description,
            SupplyStatus = productSupply.SupplyStatus,
            SupplierId = productSupply.SupplierId
        };
    }
}
