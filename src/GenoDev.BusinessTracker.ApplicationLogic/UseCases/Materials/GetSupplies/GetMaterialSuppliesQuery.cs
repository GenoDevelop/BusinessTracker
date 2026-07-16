using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;

public record MaterialSupplyDto(
    Guid Id,
    string SupplierName,
    DateTime OrderDate,
    decimal TotalNetPrice,
    decimal TotalGrossPrice,
    MaterialSupplyStatus Status);

public record GetMaterialSuppliesQuery(
    int PageIndex,
    int PageSize,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<PagedList<MaterialSupplyDto>>;
