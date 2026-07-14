using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.ProductSupplies.Create;

public class CreateProductSupplyCommandHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<CreateProductSupplyCommand, ProductSupplyDto>
{
    public async Task<ProductSupplyDto> Handle(CreateProductSupplyCommand request, CancellationToken cancellationToken)
    {
        var productSupply = new ProductSupply
        {
            ProductId = request.ProductId,
            SupplierId = request.SupplierId,
            BuyPriceNet = request.BuyPriceNet,
            BuyPriceGross = request.BuyPriceGross,
            Quantity = request.Quantity,
            SupplyStatus = request.SupplyStatus,
            BuyTime = request.BuyTime?.ToUniversalTime(),
            Description = request.Description
        };

        dbContext.ProductSupplies.Add(productSupply);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductSupplyDto
        {
            Id = productSupply.Id,
            ProductId = productSupply.ProductId,
            SupplierId = productSupply.SupplierId,
            BuyPriceNet = productSupply.BuyPriceNet,
            BuyPriceGross = productSupply.BuyPriceGross,
            Quantity = productSupply.Quantity,
            SupplyStatus = productSupply.SupplyStatus,
            BuyTime = productSupply.BuyTime,
            Description = productSupply.Description
        };
    }
}
