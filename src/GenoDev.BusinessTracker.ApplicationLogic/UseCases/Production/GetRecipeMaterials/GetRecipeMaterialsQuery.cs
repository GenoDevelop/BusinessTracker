using GenoDev.BusinessTracker.ApplicationLogic;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;

public record GetRecipeMaterialsQuery(
    Guid RecipeId,
    int PageIndex = 0,
    int PageSize = 50,
    string? MaterialNameFilter = null,
    string? EanFilter = null,
    double? AmountFilterValue = null,
    NumericOperator? AmountOperator = null,
    RecipeMaterialSortBy SortBy = RecipeMaterialSortBy.MaterialName,
    bool IsDescending = false) : IRequest<PagedList<RecipeMaterialDto>>;

public record RecipeMaterialDto(
    Guid Id,
    Guid MaterialId,
    string MaterialName,
    string? Ean,
    double RequiredAmount,
    string? Unit);
