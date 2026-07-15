using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;

public record SupplierDto(
    Guid Id,
    string Name,
    string? Nip,
    string? Description,
    string? WebsiteUrl);

public record GetSuppliersQuery(
    int PageIndex,
    int PageSize,
    SupplierSortBy SortBy = SupplierSortBy.Name,
    bool IsDescending = false) : IRequest<PagedList<SupplierDto>>;
