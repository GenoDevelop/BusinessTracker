using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetSuppliedProductSuppliers;

public record GetSuppliedProductSuppliersQuery(Guid ProductId) : IRequest<List<SupplierDto>>;