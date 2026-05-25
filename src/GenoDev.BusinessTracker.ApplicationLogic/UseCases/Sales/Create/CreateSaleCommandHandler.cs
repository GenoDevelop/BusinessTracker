using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.Create;

public record CreateProductSaleInput(Guid ProductId, Guid TaxRateId, double Quantity, decimal SalePriceGross, string? Description);
public record CreateSalesCostsAdjustmentInput(string CostName, decimal AdjustmentValueGross, string? Description);

public record CreateSaleCommand(
    DateTimeOffset SaleTime,
    string? Description,
    string? SaleIdentifier,
    string? PaymentIdentifier,
    List<CreateProductSaleInput> ProductSales,
    List<CreateSalesCostsAdjustmentInput> SalesCostsAdjustments) : IRequest<SaleDto>;

public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, SaleDto>
{
    private readonly IBusinessTrackerDbContext _context;
    private readonly ISaleService _saleService;

    public CreateSaleCommandHandler(IBusinessTrackerDbContext context, ISaleService saleService)
    {
        _context = context;
        _saleService = saleService;
    }

    public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleTime = request.SaleTime,
            Description = request.Description,
            SaleIdentifier = request.SaleIdentifier,
            PaymentIdentifier = request.PaymentIdentifier
        };

        _context.Sales.Add(sale);

        foreach (var ps in request.ProductSales)
        {
            _saleService.AddProductSale(sale, ps.ProductId, ps.TaxRateId, ps.Quantity, ps.SalePriceGross, ps.Description);
        }

        foreach (var sca in request.SalesCostsAdjustments)
        {
            _saleService.AddSalesCostsAdjustment(sale, sca.CostName, sca.AdjustmentValueGross, sca.Description);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SaleDto(
            sale.Id,
            sale.SaleTime,
            sale.Description,
            sale.SaleIdentifier,
            sale.PaymentIdentifier,
            sale.ProductSales.Select(x => new ProductSaleDto(x.Id, x.ProductId, x.TaxRateId, x.Quantity, x.SalePriceGross, x.Decription)).ToList(),
            sale.SalesCostsAdjustments.Select(x => new SalesCostsAdjustmentDto(x.Id, x.CostName, x.AdjustmentValueGross, x.Description)).ToList());
    }
}
