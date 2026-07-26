using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;

public record SupplyDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    decimal ItemsTotalNetPrice,
    decimal ItemsTotalGrossPrice,
    decimal ShippingNetPrice,
    decimal ShippingGrossPrice,
    MaterialSupplyStatus Status,
    string? InvoiceNo,
    string? Description,
    string? WebsiteUrl);

public record GetSuppliesQuery(
    int PageIndex,
    int PageSize,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<PagedList<SupplyDto>>;
