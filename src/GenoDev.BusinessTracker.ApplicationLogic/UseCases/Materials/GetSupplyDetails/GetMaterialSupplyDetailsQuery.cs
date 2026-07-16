using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;

public record MaterialSupplyDetailsDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime OrderDate,
    MaterialSupplyStatus Status,
    decimal TotalNetPrice,
    decimal TotalGrossPrice,
    string? InvoiceNo,
    string? Description,
    string? WebsiteUrl);

public record GetMaterialSupplyDetailsQuery(Guid Id) : IRequest<MaterialSupplyDetailsDto?>;
