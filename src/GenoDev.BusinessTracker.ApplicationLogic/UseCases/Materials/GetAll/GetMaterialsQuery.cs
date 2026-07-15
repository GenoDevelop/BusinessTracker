using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;

public record MaterialDto(
    Guid Id,
    string Name,
    string? Ean,
    string? Description,
    string? Unit,
    double Amount);

public record GetMaterialsQuery(
    int PageIndex,
    int PageSize,
    MaterialSortBy SortBy = MaterialSortBy.Name,
    bool IsDescending = false) : IRequest<PagedList<MaterialDto>>;
