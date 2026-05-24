using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

public record GetAllSuppliersQuery(int Page, int PageSize, SupplierSortBy? SortBy = null, bool IsDescending = false) : IRequest<PagedList<SupplierDto>>;
