using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetSuppliedProductSuppliers;

public class GetSuppliedProductSuppliersQueryHandler(IBusinessTrackerDbContext dbContext) : IRequestHandler<GetSuppliedProductSuppliersQuery, List<SupplierDto>>
{
    public async Task<List<SupplierDto>> Handle(GetSuppliedProductSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ProductSupplies
            .Where(x => x.ProductId == request.ProductId)
            .Select(x => x.Supplier)
            .Distinct()
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                SupplierName = x.SupplierName,
                Description = x.Description
            })
            .ToListAsync(cancellationToken);
    }
}