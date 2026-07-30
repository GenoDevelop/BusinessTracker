using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;

public record SupplyDetailsDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    MaterialSupplyStatus Status,
    decimal TotalNetPrice,
    decimal TotalGrossPrice,
    decimal ShippingNetPrice,
    decimal ShippingGrossPrice,
    string? InvoiceNo,
    string? Description,
    string? WebsiteUrl);

public record GetSupplyDetailsQuery(Guid Id) : IRequest<SupplyDetailsDto?>;
